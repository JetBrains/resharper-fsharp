namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [| typeof<LambdaCanBeReplacedWithBuiltinFunctionWarning>
                                                typeof<LambdaCanBeReplacedWithInnerExpressionWarning>
                                                typeof<LambdaCanBeSimplifiedWarning>
                                                typeof<RedundantApplicationWarning> |])>]
type LambdaAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec patIsUsed (nameUsages: OneToListMap<string, ITreeNode>) (excludedUseExpr: ITreeNode) (pat: IFSharpPattern) =
        match pat.IgnoreInnerParens() with
        | :? ITuplePat as tuplePat -> Seq.exists (patIsUsed nameUsages excludedUseExpr) tuplePat.PatternsEnumerable
        | :? ILocalReferencePat as refPat ->
            nameUsages.GetValuesSafe(refPat.SourceName)
            |> Seq.exists (fun u -> isNotNull u && not (excludedUseExpr.Contains(u)))
        | _ -> false

    let rec compareArg (pat: IFSharpPattern) (arg: IFSharpExpression) =
        match pat.IgnoreInnerParens(), arg.IgnoreInnerParens() with
        | :? ITuplePat as pat, (:? ITupleExpr as expr) ->
            not pat.IsStruct = not expr.IsStruct &&
            compareArgsSeq pat.PatternsEnumerable expr.ExpressionsEnumerable

        | :? ILocalReferencePat as pat, (:? IReferenceExpr as expr) ->
            let patReferenceName = pat.ReferenceName
            if patReferenceName.IsQualified || expr.IsQualified then false else

            let patName = patReferenceName.ShortName
            if patName.IsEmpty() || not (Char.IsLower(patName[0])) then false else

            patName = expr.ShortName

        | :? IUnitPat, :? IUnitExpr -> true
        | _ -> false

    and compareArgsSeq (pats: IFSharpPattern seq) (args: IFSharpExpression seq) =
        if args.IsEmpty() then pats.IsEmpty() else
        if pats.IsEmpty() then false else

        compareArg (Seq.head pats) (Seq.head args) && compareArgsSeq (Seq.tail pats) (Seq.tail args)

    and compareArgs (pats: TreeNodeCollection<_>) (expr: IFSharpExpression) =
        let rec compareArgsRec (expr: IFSharpExpression) i nameUsages =
            let hasMatches = i > 0
            match expr.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> pats.Count ->
                let pat = pats[pats.Count - 1 - i]
                let argExpr = app.ArgumentExpression

                if not (compareArg pat argExpr) then
                    hasMatches, false, app :> IFSharpExpression
                else
                    let funExpr = app.FunctionExpression
                    let usedNames =
                        if isNotNull nameUsages then nameUsages else
                        FSharpNamingService.getUsedNamesUsages [funExpr] EmptyList.Instance null false

                    if not (patIsUsed usedNames argExpr pat) then
                        compareArgsRec funExpr (i + 1) usedNames
                    else
                        hasMatches, false, app :> _

            | :? IReferenceExpr as r when
                    let name = r.ShortName
                    name = SharedImplUtil.MISSING_DECLARATION_NAME || PrettyNaming.IsOperatorDisplayName name ->
                false, false, null

            | x -> hasMatches, i = pats.Count, x

        compareArgsRec expr 0 null

    let isWarningApplicable (lambda: ILambdaExpr) (exprToReplace: IFSharpExpression) isFSharp6Supported =
        let lambda = lambda.IgnoreParentParens()
        let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(lambda)
        let argExpr = if isNull binaryExpr then lambda else binaryExpr :> _
        let appTuple = TupleExprNavigator.GetByExpression(argExpr)
        let app = getArgsOwner argExpr

        let reference = getReference app
        if not (app :? IPrefixAppExpr && isNotNull reference) then true else

        match reference.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as m when m.IsMember ->
            let lambdaPos = if isNotNull appTuple then appTuple.Expressions.IndexOf(argExpr) else 0

            let parameterGroups = m.CurriedParameterGroups
            if parameterGroups.Count = 0 then true else

            let args = parameterGroups[0]
            if args.Count <= lambdaPos then true else

            let argDecl = args[lambdaPos]
            let argDeclType = argDecl.Type
            let argIsDelegate = argDeclType.HasTypeDefinition && (getAbbreviatedEntity argDeclType.TypeDefinition).IsDelegate
            if argIsDelegate && not isFSharp6Supported then false else

            match exprToReplace.IgnoreInnerParens() with
            | :? IReferenceExpr as ref ->
                let symbol = ref.Reference.GetFcsSymbol()
                match symbol with
                | :? FSharpMemberOrFunctionOrValue as x ->
                    x.IsFunction || not x.IsMember ||
                    not argIsDelegate &&
                    match exprToReplace.FSharpFile.GetParseAndCheckResults(true, "FSharpParameterInfoContextFactory.getMethods") with
                    | None -> true
                    | Some results ->

                    let referenceOwner = reference.GetElement()
                    let names = 
                        match referenceOwner with
                        | :? IFSharpQualifiableReferenceOwner as referenceOwner -> List.ofSeq referenceOwner.Names
                        | _ -> [reference.GetName()]

                    let identifier = referenceOwner.FSharpIdentifier
                    if isNull identifier then true else

                    let endCoords = identifier.GetDocumentStartOffset().ToDocumentCoords()
                    let line = int endCoords.Line + 1
                    let column = int endCoords.Column + 1
                
                    let checkResults = results.CheckResults
                    let methodGroupItems = checkResults.GetMethods(line, column, "", Some names).Methods
                    methodGroupItems.Length = 1
                | _ -> true
            | _ -> true
        | _ -> true

    let isApplicable (expr: IFSharpExpression) (pats: TreeNodeCollection<IFSharpPattern>) =
        match expr with
        | :? IPrefixAppExpr
        | :? IReferenceExpr
        | :? ITupleExpr
        | :? IUnitExpr -> not (pats.Count = 1 && pats.First().IgnoreInnerParens() :? IUnitPat)
        | _ -> false

    override x.Run(lambda, data, consumer) =
        let isFsharp60Supported = data.IsFSharp60Supported
        let expr = lambda.Expression.IgnoreInnerParens()
        let pats = lambda.Patterns

        if not (isApplicable expr pats) then () else

        let warning =
            match compareArgs pats expr with
            | true, true, replaceCandidate ->
                if isWarningApplicable lambda replaceCandidate isFsharp60Supported then
                    LambdaCanBeReplacedWithInnerExpressionWarning(lambda, replaceCandidate) :> IHighlighting
                else null
            | true, false, replaceCandidate ->
                if isWarningApplicable lambda replaceCandidate isFsharp60Supported then
                    LambdaCanBeSimplifiedWarning(lambda, replaceCandidate) :> _
                else null
            | _ ->

            if pats.Count = 1 then
                let pat = pats[0].IgnoreInnerParens()
                if compareArg pat expr then
                    let expr = lambda.IgnoreParentParens()
                    let mutable funExpr = Unchecked.defaultof<_>
                    let mutable arg = Unchecked.defaultof<_>
                    if isFunctionInApp expr &funExpr &arg then
                        RedundantApplicationWarning(funExpr, arg) :> _
                    else
                        if isWarningApplicable lambda null isFsharp60Supported then
                            LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "id") :> _
                        else null
                else
                    match pat with
                    | :? ITuplePat as pat when not pat.IsStruct && pat.PatternsEnumerable.CountIs(2) ->
                        let tuplePats = pat.Patterns
                        if compareArg tuplePats[0] expr then
                            if isWarningApplicable lambda null isFsharp60Supported then
                                LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "fst") :> _
                            else null
                        elif compareArg tuplePats[1] expr then
                            if isWarningApplicable lambda null isFsharp60Supported then
                                LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "snd") :> _
                            else null
                        else null
                    | _ -> null

            else null

        if isNotNull warning then consumer.AddHighlighting(warning)
