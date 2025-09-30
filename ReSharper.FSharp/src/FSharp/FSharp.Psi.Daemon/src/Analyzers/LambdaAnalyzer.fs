namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open System
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [| typeof<DotLambdaCanBeUsedWarning>
                                                typeof<LambdaCanBeReplacedWithBuiltinFunctionWarning>
                                                typeof<LambdaCanBeReplacedWithInnerExpressionWarning>
                                                typeof<LambdaCanBeSimplifiedWarning>
                                                typeof<RedundantApplicationWarning> |])>]
type LambdaAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec patIsUsed (nameUsages: OneToListMap<string, ITreeNode>) (excludedUseExpr: ITreeNode) (pat: IFSharpPattern) =
        match pat.IgnoreInnerParens() with
        | :? ITuplePat as tuplePat ->
            Seq.exists (patIsUsed nameUsages excludedUseExpr) tuplePat.PatternsEnumerable

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

        exprType <> lambdaReturnType

    let isLambdaArgOwnerSupported (lambda: IFSharpExpression) delegatesConversionSupported
            (replacementExprSymbol: FSharpSymbol voption) =
        isNull (QuoteExprNavigator.GetByQuotedExpression(lambda.IgnoreParentParens())) &&

        let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(lambda)
        let argExpr = if isNull binaryExpr then lambda else binaryExpr :> _
        let argExpr = argExpr.IgnoreParentParens()
        let appTuple = TupleExprNavigator.GetByExpression(argExpr)
        let app = getArgsOwner argExpr

        let reference = getReference app
        if not (app :? IPrefixAppExpr) || isNull reference then true else

        if isNotNull binaryExpr && not (FSharpArgumentsUtil.IsNamedArgSyntactically(binaryExpr)) then true else

        match reference.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as m when m.IsMember ->
            let lambdaPos = if isNotNull appTuple then appTuple.Expressions.IndexOf(argExpr) else 0

            let parameterGroups = m.CurriedParameterGroups
            if parameterGroups.Count = 0 then true else

            let parameters = parameterGroups[0]
            if parameters.Count <= lambdaPos then true else

            let parameterDecl = parameters[lambdaPos]
            let parameterType = parameterDecl.Type
            let parameterIsDelegateOrExprDelegate =
                parameterType.HasTypeDefinition &&
                let abbreviatedType = getAbbreviatedEntity parameterType.TypeDefinition
                abbreviatedType.IsDelegate ||

                abbreviatedType.LogicalName = "Expression`1" &&
                match abbreviatedType.TryFullName with
                | Some "System.Linq.Expressions.Expression`1" -> true
                | _ -> false

            // If the lambda is passed instead of a delegate,
            // then in F# < 6.0 there is almost never an implicit cast for the lambda simplification
            if parameterIsDelegateOrExprDelegate && not delegatesConversionSupported then false else

            match replacementExprSymbol with
            | ValueSome (:? FSharpMemberOrFunctionOrValue as x) ->
                // If the lambda simplification does not convert it to a method group,
                // for example, if the body of the lambda does not consist of a method call,
                // then everything is OK
                x.IsFunction || not x.IsMember ||

                not parameterIsDelegateOrExprDelegate &&

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

    let tryCreateWarning (ctor: ILambdaExpr * 'a -> #IHighlighting) (lambda: ILambdaExpr, replacementExpr: 'a as arg)
            isFSharp6Supported =
        if isFSharp6Supported && hasExplicitConversion lambda then null else

        let lambda = lambda.IgnoreParentParens()

        let replacementRefExpr = replacementExpr.As<IFSharpExpression>().IgnoreInnerParens().As<IReferenceExpr>()
        let replacementExprSymbol =
            if isNull replacementRefExpr then ValueNone else ValueSome(replacementRefExpr.Reference.GetFcsSymbol())

        match replacementExprSymbol with
        | ValueSome (:? FSharpEntity as entity) when (getAbbreviatedEntity entity).IsDelegate -> null
        | ValueSome (:? FSharpActivePatternCase) -> null
        | _ ->

        let outerReferenceCheck = isLambdaArgOwnerSupported lambda isFSharp6Supported replacementExprSymbol
        if not outerReferenceCheck then null else

        match replacementExprSymbol with
        | ValueSome (:? FSharpMemberOrFunctionOrValue as x) when x.IsMember ->
            let hasOptionalArg =
                x.CurriedParameterGroups
                |> Seq.concat
                |> Seq.exists (fun x -> x.IsOptionalArg)
            if hasOptionalArg then null else ctor arg

        | _ -> ctor arg

    let tryCreateWarningForBuiltInFun ctor (lambda: ILambdaExpr, funName: string as arg) isFSharp6Supported =
        if not (resolvesToPredefinedFunction lambda.RArrow funName "LambdaAnalyzer") then null else
        tryCreateWarning ctor arg isFSharp6Supported

    let rec containsForcedCalculations (expression: IFSharpExpression) =
        let mutable containsForcedCalculations = false
        let mutable prefixAppExprContext: IPrefixAppExpr = null

        let typeIsReadOnly (expr: IFSharpExpression) =
            let fcsType = expr.TryGetFcsType()
            isNotNull fcsType && isReadOnly fcsType

        let processor =
            { new IRecursiveElementProcessor with
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

    let getRootRefExpr (expr: IFSharpExpression) =
        let rec inner (prevExpr: IFSharpExpression) (expr: IFSharpExpression) =
            // parens allowed only for (x).M
            match expr.IgnoreInnerParens() with
            | :? IReferenceExpr as expr when isNull expr.Qualifier ->
                if isNull prevExpr || isIndexerLikeAppExpr prevExpr then null else expr
            | _ ->

            match expr with
            | :? IPrefixAppExpr as prefixApp when prefixApp.IsHighPrecedence ->
                // '...App x y' not supported
                if prefixApp.IsIndexerLike && prefixApp.FunctionExpression.IgnoreInnerParens() :? IPrefixAppExpr then null
                else inner prefixApp prefixApp.InvokedExpression

            | :? IQualifiedExpr as expr -> inner expr expr.Qualifier
            | _ -> null

        inner null expr

    let getRootRefExprIfCanBeConvertedToDotLambda (pat: ILocalReferencePat) (lambda: ILambdaExpr) =
        let isFSharp81Supported = FSharpLanguageLevel.isFSharp81Supported lambda
        let expr = lambda.Expression.IgnoreInnerParens()
        if not expr.IsSingleLine then null else

        let rootRefExpr = getRootRefExpr expr
        if isNull rootRefExpr ||
           rootRefExpr.ShortName <> pat.SourceName ||
           not (isFSharp81Supported || isContextWithoutWildPats expr) then null else

        let patSymbol = pat.GetFcsSymbol()
        let mutable convertingUnsupported = false
        expr.ProcessDescendants({ new IRecursiveElementProcessor with
            member x.ProcessingIsFinished = convertingUnsupported
            member x.InteriorShouldBeProcessed(treeNode) = true
            member x.ProcessAfterInterior(treeNode) = ()

            member x.ProcessBeforeInterior(treeNode) =
                if treeNode == rootRefExpr then () else
                match treeNode with
                | :? IDotLambdaExpr -> convertingUnsupported <- not isFSharp81Supported
                | :? IReferenceExpr as ref when not ref.IsQualified ->
                    if ref.ShortName <> rootRefExpr.ShortName then () else
                    let symbol = ref.Reference.GetFcsSymbol()
                    convertingUnsupported <- isNull symbol || symbol.IsEffectivelySameAs(patSymbol)
                | _ -> ()
        })
        if convertingUnsupported then null
        //TODO: workaround for https://github.com/dotnet/fsharp/issues/16305
        elif isFSharp81Supported || isLambdaArgOwnerSupported lambda false ValueNone then rootRefExpr
        else null

    let isApplicable (expr: IFSharpExpression) (pats: TreeNodeCollection<IFSharpPattern>) =
        match expr with
        | :? IPrefixAppExpr
        | :? IReferenceExpr
        | :? ITupleExpr
        | :? IItemIndexerExpr
        | :? IUnitExpr -> not (pats.Count = 1 && pats.First().IgnoreInnerParens() :? IUnitPat)
        | _ -> false

    override x.Run(lambda, data, consumer) =
        let isFSharp60Supported = data.IsFSharp60Supported
        let isFSharp80Supported = data.IsFSharp80Supported
        let expr = lambda.Expression.IgnoreInnerParens()
        let pats = lambda.Patterns

        if not (isApplicable expr pats) then () else

        let warning: IHighlighting =
            match compareArgs pats expr with
            | true, true, replaceCandidate ->
                if containsForcedCalculations replaceCandidate then null else

                tryCreateWarning LambdaCanBeReplacedWithInnerExpressionWarning (lambda, replaceCandidate)
                    isFSharp60Supported :> _

            | true, false, replaceCandidate ->
                tryCreateWarning LambdaCanBeSimplifiedWarning (lambda, replaceCandidate) isFSharp60Supported :> _

            | _ ->

            if pats.Count = 1 then
                let pat = pats[0].IgnoreInnerParens()
                if compareArg pat expr then
                    let expr = lambda.IgnoreParentParens()
                    let mutable funExpr = Unchecked.defaultof<_>
                    let mutable arg = Unchecked.defaultof<_>
                    if isFunctionInAppExpr expr &funExpr &arg then
                        RedundantApplicationWarning(funExpr, arg) :> _
                    else
                        tryCreateWarningForBuiltInFun LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda,  "id")
                            isFSharp60Supported :> _
                else
                    match pat with
                    | :? ITuplePat as pat when not pat.IsStruct && pat.PatternsEnumerable.CountIs(2) ->
                        let tuplePats = pat.Patterns
                        if compareArg tuplePats[0] expr then
                            tryCreateWarningForBuiltInFun LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "fst")
                                isFSharp60Supported :> _
                        elif compareArg tuplePats[1] expr then
                            tryCreateWarningForBuiltInFun LambdaCanBeReplacedWithBuiltinFunctionWarning (lambda, "snd")
                                isFSharp60Supported :> _
                        else
                            null

                    | :? ILocalReferencePat as pat when isFSharp80Supported ->
                        match getRootRefExprIfCanBeConvertedToDotLambda pat lambda with
                        | null -> null
                        | referenceExpr -> DotLambdaCanBeUsedWarning(lambda, referenceExpr)

                    | _ -> null

            else
                null

        if isNotNull warning then
            consumer.AddHighlighting(warning)
