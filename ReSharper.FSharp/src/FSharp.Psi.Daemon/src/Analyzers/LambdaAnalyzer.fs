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
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

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

    let hasExplicitConversion (lambda: ILambdaExpr) =
        let expr = lambda.Expression.IgnoreInnerParens()

        let lambdaType = lambda.TryGetFcsType()
        if isNull lambdaType then false else

        let lambdaParamsCount = lambda.Patterns.Count
        let mutable lambdaReturnType = lambdaType
        let mutable i = lambdaParamsCount

        while i > 0 && lambdaReturnType.IsFunctionType do
            lambdaReturnType <- lambdaReturnType.GenericArguments[1]
            i <- i - 1

        let exprType =
            match expr with
            | :? IPrefixAppExpr as prefixAppExpr ->
                match prefixAppExpr.FunctionExpression with
                | :? IReferenceExpr as referenceExpr ->
                    match referenceExpr.Reference.GetFcsSymbol() with
                    | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsMember ->
                        mfv.ReturnParameter.Type
                    | _ -> expr.TryGetFcsType()
                | _ -> expr.TryGetFcsType()
            | _ -> expr.TryGetFcsType()

        isNotNull exprType && not exprType.IsUnresolved &&
        not exprType.IsGenericParameter && not lambdaReturnType.IsGenericParameter &&

        // TODO: find a better way to compare types, since regular comparison doesn't work in tests
        exprType.Format(FSharpDisplayContext.Empty) <> lambdaReturnType.Format(FSharpDisplayContext.Empty)

    let tryCreateWarning (ctor: ILambdaExpr * 'a -> #IHighlighting) (lambda: ILambdaExpr, replacementExpr: 'a as arg) isFSharp6Supported =
        if isFSharp6Supported && hasExplicitConversion(lambda) then null else

        let lambda = lambda.IgnoreParentParens()

        let replacementRefExpr = replacementExpr.As<IFSharpExpression>().IgnoreInnerParens().As<IReferenceExpr>()
        let replacementExprSymbol =
            if isNull replacementRefExpr then ValueNone else ValueSome(replacementRefExpr.Reference.GetFcsSymbol())

        match replacementExprSymbol with
        | ValueSome (:? FSharpEntity as entity) when (getAbbreviatedEntity entity).IsDelegate -> null
        | ValueSome (:? FSharpActivePatternCase) -> null
        | _ ->

        let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(lambda)
        let argExpr = if isNull binaryExpr then lambda else binaryExpr :> _
        let argExpr = argExpr.IgnoreParentParens()
        let appTuple = TupleExprNavigator.GetByExpression(argExpr)
        let app = getArgsOwner argExpr

        let outerReferenceCheck =
            let reference = getReference app
            if not (app :? IPrefixAppExpr) || isNull reference then true else

            if isNotNull binaryExpr &&
               not (hasNamedArgStructure binaryExpr && isTopLevelArg binaryExpr) then true else

            match reference.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as m when m.IsMember ->
                let lambdaPos = if isNotNull appTuple then appTuple.Expressions.IndexOf(argExpr) else 0

                let parameterGroups = m.CurriedParameterGroups
                if parameterGroups.Count = 0 then true else

                let parameters = parameterGroups[0]
                if parameters.Count <= lambdaPos then true else

                let parameterDecl = parameters[lambdaPos]
                let parameterType = parameterDecl.Type
                let parameterIsDelegate =
                    parameterType.HasTypeDefinition && (getAbbreviatedEntity parameterType.TypeDefinition).IsDelegate

                // If the lambda is passed instead of a delegate,
                // then in F# < 6.0 there is almost never an implicit cast for the lambda simplification
                if parameterIsDelegate && not isFSharp6Supported then false else

                match replacementExprSymbol with
                | ValueSome (:? FSharpMemberOrFunctionOrValue as x) ->
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
                | _ -> true
            | _ -> true

        if not outerReferenceCheck then null else

        match replacementExprSymbol with
        | ValueSome (:? FSharpMemberOrFunctionOrValue as x) when x.IsMember ->
            let hasOptionalArg =
                x.CurriedParameterGroups
                |> Seq.concat
                |> Seq.exists (fun x -> x.IsOptionalArg)
            if hasOptionalArg then null else ctor arg
        | _ -> ctor arg

    let rec containsForcedCalculations (expression: IFSharpExpression) =
        let mutable containsForcedCalculations = false
        let mutable prefixAppExprContext: IPrefixAppExpr = null

        let typeIsReadOnly (expr: IFSharpExpression) =
            let fcsType = expr.TryGetFcsType()
            isNotNull fcsType && isReadOnly fcsType

        let processor = { new IRecursiveElementProcessor with
            member x.ProcessingIsFinished = containsForcedCalculations
            member x.InteriorShouldBeProcessed(treeNode) = not (treeNode :? ILambdaExpr)
            member x.ProcessAfterInterior(treeNode) = ()
            member x.ProcessBeforeInterior(treeNode) =
                match treeNode with
                | :? INewExpr
                | :? IForLikeExpr
                | :? ISetExpr
                | :? IWhileExpr
                | :? IBinaryAppExpr ->
                    containsForcedCalculations <- true
                | :? IIndexerExpr as indexer when not (typeIsReadOnly indexer.Qualifier) ->
                    containsForcedCalculations <- true
                | :? IPrefixAppExpr as prefixAppExpr ->
                    if prefixAppExpr.IsIndexerLike && not (typeIsReadOnly prefixAppExpr.FunctionExpression) then
                        containsForcedCalculations <- true
                    elif isNull (PrefixAppExprNavigator.GetByFunctionExpression(prefixAppExpr)) then
                        prefixAppExprContext <- prefixAppExpr
                | :? IReferenceExpr as referenceExpr ->
                    containsForcedCalculations <-
                        let fcsSymbol = referenceExpr.Reference.GetFcsSymbol()
                        isNull fcsSymbol ||
                        match fcsSymbol with
                        | :? FSharpMemberOrFunctionOrValue as m ->
                            (m.IsProperty|| m.IsTypeFunction) && (isNull prefixAppExprContext || not prefixAppExprContext.IsIndexerLike) ||
                            m.IsMutable ||
                            isNotNull prefixAppExprContext && (m.IsConstructor ||
                            (m.IsFunction || m.IsMethod) &&
                             m.CurriedParameterGroups.Count <= prefixAppExprContext.Arguments.Count)
                        | :? FSharpUnionCase when isNotNull prefixAppExprContext -> true
                        | :? FSharpEntity as e when e.IsDelegate -> true
                        | _ -> false
                    prefixAppExprContext <- null
                | _ -> ()
            }

        expression.ProcessThisAndDescendants(processor)
        containsForcedCalculations

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

        let warning: IHighlighting =
            match compareArgs pats expr with
            | true, true, replaceCandidate ->
                if containsForcedCalculations replaceCandidate then null else
                tryCreateWarning LambdaCanBeReplacedWithInnerExpressionWarning (lambda, replaceCandidate) isFsharp60Supported :> _
            | true, false, replaceCandidate ->
                tryCreateWarning LambdaCanBeSimplifiedWarning (lambda, replaceCandidate) isFsharp60Supported :> _
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
                        tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda,  "id") isFsharp60Supported :> _
                else
                    match pat with
                    | :? ITuplePat as pat when not pat.IsStruct && pat.PatternsEnumerable.CountIs(2) ->
                        let tuplePats = pat.Patterns
                        if compareArg tuplePats[0] expr then
                            tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "fst") isFsharp60Supported :> _
                        elif compareArg tuplePats[1] expr then
                            tryCreateWarning LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "snd") isFsharp60Supported :> _
                        else null
                    | _ -> null

            else null

        if isNotNull warning then consumer.AddHighlighting(warning)
