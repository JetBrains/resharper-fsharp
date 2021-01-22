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

        | :? IAttribPat
        | :? ITypedPat
        | :? IAsPat -> 5

        | :? IParametersOwnerPat -> 6

        | :? ILambdaParametersList
        | :? IParametersPatternDeclaration -> 7

        // The rest of the patterns.
        | :? IFSharpPattern -> 8

        | _ -> 0

    let getParentPatternFromLeftSide (pat: IFSharpPattern): IFSharpPattern =
        let listConsPat = ListConsPatNavigator.GetByHeadPattern(pat)
        if isNotNull listConsPat then listConsPat :> _ else

        let attribPat = AttribPatNavigator.GetByPattern(pat)
        if isNotNull attribPat then attribPat :> _ else

        null

    let rec isAtCompoundPatternRightSide pat =
        if isNull pat then false else

        if isNotNull (OrPatNavigator.GetByPattern2(pat)) then true else
        if isNotNull (ListConsPatNavigator.GetByTailPattern(pat)) then true else

        let tuplePat = TuplePatNavigator.GetByPattern(pat)
        if isNotNull tuplePat && tuplePat.PatternsEnumerable.FirstOrDefault() != pat then true else

        let andsPat = AndsPatNavigator.GetByPattern(pat)
        if isNotNull andsPat && andsPat.PatternsEnumerable.FirstOrDefault() != pat then true else

        let parent = getParentPatternFromLeftSide pat
        isAtCompoundPatternRightSide parent

    let checkPrecedence pat parent =
        precedence pat < precedence parent

    let needsParens (context: IFSharpPattern) (fsPattern: IFSharpPattern) =
        match fsPattern with
        | :? IListConsPat ->
            isNotNull (ListConsPatNavigator.GetByHeadPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IAsPat ->
            isAtCompoundPatternRightSide context ||
            checkPrecedence fsPattern context.Parent

        | :? ITuplePat ->
            isNotNull (TuplePatNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IParametersOwnerPat ->
            isNotNull (BindingNavigator.GetByHeadPattern(context)) ||
            isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? ITypedPat
        | :? IAttribPat ->
            isNotNull (BindingNavigator.GetByHeadPattern(context)) ||
            isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
            checkPrecedence fsPattern context.Parent

        | :? IWildPat -> false

        | _ ->

        // todo: add code style setting
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(context)
        isNotNull parametersOwnerPat && getNextSibling parametersOwnerPat.ReferenceName == context ||

        let parameterDecl = ParametersPatternDeclarationNavigator.GetByPattern(context)
        isNotNull (ConstructorDeclarationNavigator.GetByParametersPatternDeclaration(parameterDecl)) ||

        // todo: add code style setting
        let memberDeclaration = MemberDeclarationNavigator.GetByParametersPattern(parameterDecl)
        isNotNull memberDeclaration && getNextSibling memberDeclaration.NameIdentifier == parameterDecl ||
        isNotNull memberDeclaration && getNextSibling memberDeclaration.TypeParameterList == parameterDecl ||

        checkPrecedence fsPattern context.Parent

    override x.Run(parenPat, _, consumer) =
        let innerPattern = parenPat.Pattern
        if isNull innerPattern then () else

        let context = parenPat.IgnoreParentParens()

        if innerPattern :? IParenPat || not (needsParens context innerPattern) && innerPattern.IsSingleLine then
            consumer.AddHighlighting(RedundantParenPatWarning(parenPat))
