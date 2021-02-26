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
        | :? IAsPat -> 1
        | :? IOrPat -> 2
        | :? IAndsPat -> 3
        | :? ITuplePat -> 4
        | :? IListConsPat -> 5

        | :? IAttribPat
        | :? ITypedLikePat -> 6

        | :? IParametersOwnerPat -> 7

        | :? ILambdaParametersList
        | :? IParametersPatternDeclaration -> 8

        // The rest of the patterns.
        | :? IFSharpPattern -> 9

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

    let rec compoundPatternNeedsParens (strictContext: ITreeNode) (fsPattern: IFSharpPattern) =
        if isNull strictContext then false else

        match fsPattern with
        | :? IAsPat as asPat -> compoundPatternNeedsParens strictContext asPat.Pattern
        | :? ITuplePat as tuplePat -> Seq.exists (compoundPatternNeedsParens strictContext) tuplePat.Patterns

        | :? IAttribPat
        | :? ITypedLikePat -> true

        | _ -> false

    let checkPrecedence (context: IFSharpPattern) pat =
        precedence pat < precedence context.Parent

    let getBindingPattern (context: IFSharpPattern) =
        let rec loop seenTuple (fsPattern: IFSharpPattern) =
            let tuplePat = TuplePatNavigator.GetByPattern(fsPattern)
            if not seenTuple && isNotNull tuplePat then loop true tuplePat else

            let tuplePat = AsPatNavigator.GetByPattern(fsPattern)
            if isNotNull tuplePat then loop seenTuple tuplePat else

            fsPattern

        loop (context :? ITuplePat) context

    let getStrictContext context =
        let bindingPattern = getBindingPattern context
        BindingNavigator.GetByHeadPattern(bindingPattern)

    let needsParens (context: IFSharpPattern) (fsPattern: IFSharpPattern) =
        match fsPattern with
        | :? IListConsPat ->
            isNotNull (ListConsPatNavigator.GetByHeadPattern(context)) ||
            checkPrecedence context fsPattern

        | :? IAsPat ->
            isAtCompoundPatternRightSide context ||
            isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||
            isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
            isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||

            let strictContext = getStrictContext context
            compoundPatternNeedsParens strictContext fsPattern

        | :? ITuplePat as tuplePat ->
            isNotNull (TuplePatNavigator.GetByPattern(context)) ||

            // todo: suggest moving parens to a single inner pattern?
            let strictContext = getStrictContext context
            compoundPatternNeedsParens strictContext fsPattern ||

            let matchClause = MatchClauseNavigator.GetByPattern(context)
            isNotNull matchClause && tuplePat.PatternsEnumerable.LastOrDefault() :? ITypedLikePat || 

            checkPrecedence context fsPattern

        | :? IParametersOwnerPat ->
            isNotNull (BindingNavigator.GetByHeadPattern(context)) ||
            isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||

            checkPrecedence context fsPattern

        | :? ITypedLikePat
        | :? IAttribPat ->
            isNotNull (BindingNavigator.GetByHeadPattern(getBindingPattern context)) ||
            isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
            isNotNull (MatchClauseNavigator.GetByPattern(context)) ||

            let tuplePat = TuplePatNavigator.GetByPattern(context)
            let matchClause = MatchClauseNavigator.GetByPattern(tuplePat)
            isNotNull matchClause && tuplePat.PatternsEnumerable.LastOrDefault() == context ||

            checkPrecedence context fsPattern

        | :? IWildPat -> false

        | _ ->

        // todo: add code style setting
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(context)
        isNotNull parametersOwnerPat && getNextSibling parametersOwnerPat.ReferenceName == context ||

        let parameterDecl = ParametersPatternDeclarationNavigator.GetByPattern(context)
        isNotNull (ConstructorDeclarationNavigator.GetByParametersDeclaration(parameterDecl)) ||

        // todo: add code style setting
        let memberDeclaration = MemberDeclarationNavigator.GetByParametersDeclaration(parameterDecl)
        isNotNull memberDeclaration && getNextSibling memberDeclaration.NameIdentifier == parameterDecl ||
        isNotNull memberDeclaration && getNextSibling memberDeclaration.TypeParameterList == parameterDecl ||

        checkPrecedence context fsPattern

    let escapesTuplePatParamDecl (context: IFSharpPattern) (innerPattern: IFSharpPattern) =
        match innerPattern with
        | :? IParenPat as parenPat ->
            match parenPat.Pattern with
            | :? ITuplePat -> isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context))
            | _ -> false
        | _ -> false

    override x.Run(parenPat, _, consumer) =
        let innerPattern = parenPat.Pattern
        if isNull innerPattern then () else

        let context = parenPat.IgnoreParentParens()
        if escapesTuplePatParamDecl context innerPattern then () else

        if innerPattern :? IParenPat || not (needsParens context innerPattern) && innerPattern.IsSingleLine then
            consumer.AddHighlighting(RedundantParenPatWarning(parenPat))
