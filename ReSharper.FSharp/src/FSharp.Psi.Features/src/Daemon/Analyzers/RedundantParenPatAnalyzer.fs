namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenPat>, HighlightingTypes = [| typeof<RedundantParenPatWarning> |])>]
type RedundantParenPatAnalyzer() =
    inherit ElementProblemAnalyzer<IParenPat>()

    let precedence (treeNode: ITreeNode) =
        match treeNode with
        | :? IOrPat -> 1
        | :? IAndsPat -> 2
        | :? IListConsPat -> 3
        | :? ITuplePat -> 4
        | :? IParametersOwnerPat -> 5
        | :? IAttribPat
        | :? ITypedPat
        | :? IAsPat -> 6
        | :? IFSharpPattern -> 7
        | _ -> 0

    let getParentViaLeftSide (pat: IFSharpPattern): IFSharpPattern =
        let listConsPat = ListConsPatNavigator.GetByHeadPattern(pat)
        if isNotNull listConsPat then listConsPat :> _ else

        let attribPat = AttribPatNavigator.GetByPattern(pat)
        if isNotNull attribPat then attribPat :> _ else

        null

    let rec isOnOrAndRightSide pat =
        if isNull pat then false else

        if isNotNull (OrPatNavigator.GetByPattern2(pat)) then true else
        if isNotNull (ListConsPatNavigator.GetByTailPattern(pat)) then true else

        let tuplePat = TuplePatNavigator.GetByPattern(pat)
        if isNotNull tuplePat && tuplePat.PatternsEnumerable.FirstOrDefault() != pat then true else

        let andsPat = AndsPatNavigator.GetByPattern(pat)
        if isNotNull andsPat && andsPat.PatternsEnumerable.FirstOrDefault() != pat then true else

        let parent = getParentViaLeftSide pat
        isOnOrAndRightSide parent

    let checkPrecedence pat parent =
        precedence pat < precedence parent

    let needsParens (context: IFSharpPattern) (fsPattern: IFSharpPattern) =
        match fsPattern with
        | :? IListConsPat ->
            isNotNull (ListConsPatNavigator.GetByHeadPattern(context)) ||
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IAsPat ->
            isOnOrAndRightSide context ||
            isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? ITuplePat ->
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IParametersOwnerPat ->
            isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||
            isNotNull (BindingNavigator.GetByHeadPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? ITypedPat
        | :? IAttribPat ->
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||
            isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IWildPat -> false

        | _ ->

        // todo: add code style setting
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(context)
        isNotNull parametersOwnerPat && getNextSibling parametersOwnerPat.ReferenceName == context ||

        checkPrecedence fsPattern context.Parent

    override x.Run(parenPat, _, consumer) =
        let innerPattern = parenPat.Pattern
        if isNull innerPattern then () else

        let context = parenPat.IgnoreParentParens()

        if innerPattern :? IParenPat || not (needsParens context innerPattern) && innerPattern.IsSingleLine then
            consumer.AddHighlighting(RedundantParenPatWarning(parenPat))
