namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
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

    let rec compareArgGroup (paramGroup: IPatternParameterDeclarationGroup) (argExpr: IFSharpExpression) =
        let paramPatterns = paramGroup.ParameterPatterns
        if paramPatterns.IsEmpty then false else

        if paramPatterns.Count > 1 then
            match argExpr.IgnoreInnerParens() with
            | :? ITupleExpr as tupleExpr when not tupleExpr.IsStruct ->
                compareArgsSeq paramPatterns tupleExpr.Expressions
            | _ -> false
        else
            compareArg paramPatterns[0] argExpr

    and compareArg (pat: IFSharpPattern) (argExpr: IFSharpExpression) =
        match pat.IgnoreInnerParens(), argExpr.IgnoreInnerParens() with
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

    and compareArgs (paramGroups: TreeNodeCollection<IPatternParameterDeclarationGroup>) (expr: IFSharpExpression) =
        let rec compareArgsRec (expr: IFSharpExpression) i nameUsages =
            let hasMatches = i > 0

            match expr.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> paramGroups.Count ->
                let paramGroup = paramGroups[paramGroups.Count - 1 - i]
                let argExpr = app.ArgumentExpression

                if not (compareArgGroup paramGroup argExpr) then
                    hasMatches, false, app :> IFSharpExpression
                else
                    let funExpr = app.FunctionExpression
                    let usedNames =
                        if isNotNull nameUsages then nameUsages else
                        FSharpNamingService.getUsedNamesUsages [funExpr] EmptyList.Instance null false

                    if not (paramGroup.ParameterPatternsEnumerable |> Seq.exists (patIsUsed usedNames argExpr)) then
                        compareArgsRec funExpr (i + 1) usedNames
                    else
                        hasMatches, false, app :> _

            | :? IReferenceExpr as r when
                    let name = r.ShortName
                    name = SharedImplUtil.MISSING_DECLARATION_NAME || PrettyNaming.IsOperatorDisplayName name ->
                false, false, null

            | x -> hasMatches, i = paramGroups.Count, x

        compareArgsRec expr 0 null

    let tryCreateWarning (ctor: ILambdaExpr * 'a -> #IHighlighting) (lambda: ILambdaExpr, replacementExpr: 'a as arg)
            isFSharp6Supported =

        let lambda = lambda.IgnoreParentParens()

        let replacementRefExpr = replacementExpr.As<IFSharpExpression>().IgnoreInnerParens().As<IReferenceExpr>()
        let replacementExprSymbol =
            if isNull replacementRefExpr then ValueNone else ValueSome(replacementRefExpr.Reference.GetFcsSymbol())

        match replacementExprSymbol with
        | ValueSome (:? FSharpEntity as entity) when (getAbbreviatedEntity entity).IsDelegate -> null
        | _ ->

        let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(lambda)
        let argExpr = if isNull binaryExpr then lambda else binaryExpr :> _
        let argExpr = argExpr.IgnoreParentParens()
        let appTuple = TupleExprNavigator.GetByExpression(argExpr)
        let app = getArgsOwner argExpr

        let reference = getReference app
        if not (app :? IPrefixAppExpr) || isNull reference then ctor arg else

        if isNotNull binaryExpr &&
           not (hasNamedArgStructure binaryExpr && isTopLevelArg binaryExpr) then ctor arg else

        match reference.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as m when m.IsMember ->
            let lambdaPos = if isNotNull appTuple then appTuple.Expressions.IndexOf(argExpr) else 0

            let parameterGroups = m.CurriedParameterGroups
            if parameterGroups.Count = 0 then ctor arg else

            let parameters = parameterGroups[0]
            if parameters.Count <= lambdaPos then ctor arg else

            let parameterDecl = parameters[lambdaPos]
            let parameterType = parameterDecl.Type
            let parameterIsDelegate =
                parameterType.HasTypeDefinition && (getAbbreviatedEntity parameterType.TypeDefinition).IsDelegate

            // If the lambda is passed instead of a delegate,
            // then in F# < 6.0 there is almost never an implicit cast for the lambda simplification 
            if parameterIsDelegate && not isFSharp6Supported then null else

            match replacementExprSymbol with
            | ValueSome (:? FSharpMemberOrFunctionOrValue as x) ->
                let isApplicable =
                    // If the lambda simplification does not convert it to a method group,
                    // for example, if the body of the lambda does not consist of a method call,
                    // then everything is OK
                    x.IsFunction || not x.IsMember ||

                    not parameterIsDelegate &&

                    // If the body of the lambda consists of a method call,
                    // and the method to which the lambda is passed has overloads,
                    // then it cannot be unambiguously determined whether the lambda can be simplified
                    match getAllMethods reference false "LambdaAnalyzer.getMethods" with
                    | None
                    | Some (_, None)
                    | Some (_, Some [])
                    | Some (_, Some [_]) -> true
                    | _ -> false
                if isApplicable then ctor arg else null
            | _ -> ctor arg
        | _ -> ctor arg

    let isApplicable (expr: IFSharpExpression) (paramGroups: TreeNodeCollection<IPatternParameterDeclarationGroup>) =
        match expr with
        | :? IPrefixAppExpr
        | :? IReferenceExpr
        | :? ITupleExpr
        | :? IUnitExpr ->
            let singleGroup = paramGroups.SingleItem
            isNull singleGroup || not (singleGroup.ParameterPatterns.SingleItem.IgnoreInnerParens() :? IUnitPat)
        | _ -> false

    override x.Run(lambda, data, consumer) =
        let isFsharp60Supported = data.IsFSharp60Supported
        let bodyExpr = lambda.Expression.IgnoreInnerParens()
        let paramGroups = lambda.PatternParameterGroups

        if not (isApplicable bodyExpr paramGroups) then () else

        let warning: IHighlighting =
            match compareArgs paramGroups bodyExpr with
            | true, true, replaceCandidate ->
                tryCreateWarning LambdaCanBeReplacedWithInnerExpressionWarning (lambda, replaceCandidate) isFsharp60Supported :> _
            | true, false, replaceCandidate ->
                tryCreateWarning LambdaCanBeSimplifiedWarning (lambda, replaceCandidate) isFsharp60Supported :> _
            | _ ->

            if paramGroups.Count <> 1 then null else

            let paramGroup = paramGroups[0]
            if compareArgGroup paramGroup bodyExpr then
                let expr = lambda.IgnoreParentParens()
                let mutable funExpr = Unchecked.defaultof<_>
                let mutable arg = Unchecked.defaultof<_>
                if isFunctionInApp expr &funExpr &arg then
                    RedundantApplicationWarning(funExpr, arg) :> _
                else
                    tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "id") isFsharp60Supported :> _
            else
                let pats = paramGroup.ParameterPatterns
                if pats.Count <> 2 then null else

                if compareArg pats[0] bodyExpr then
                    tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "fst") isFsharp60Supported :> _
                elif compareArg pats[1] bodyExpr then
                    tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "snd") isFsharp60Supported :> _
                else
                    null

        if isNotNull warning then
            consumer.AddHighlighting(warning)
