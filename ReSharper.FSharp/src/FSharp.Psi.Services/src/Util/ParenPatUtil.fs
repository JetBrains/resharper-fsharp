module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.ParenPatUtil

open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

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

let rec isAtCompoundPatternRightSide (pat: IFSharpPattern) =
    if isNull pat then false else

    if isNotNull (OrPatNavigator.GetByPattern2(pat)) then true else
    if isNotNull (ListConsPatNavigator.GetByTailPattern(pat)) then true else

    let tuplePat = TuplePatNavigator.GetByPattern(pat)
    if isNotNull tuplePat then
        tuplePat.PatternsEnumerable.FirstOrDefault() != pat || isAtCompoundPatternRightSide tuplePat else

    let andsPat = AndsPatNavigator.GetByPattern(pat)
    if isNotNull andsPat then
        andsPat.PatternsEnumerable.FirstOrDefault() != pat || isAtCompoundPatternRightSide andsPat else

    let parent = getParentPatternFromLeftSide pat
    isAtCompoundPatternRightSide parent

let isCompoundPattern (pat: IFSharpPattern) =
    match pat with
    | :? IConstPat
    | :? IListPat
    | :? INullPat
    | :? IRecordPat
    | :? IReferencePat
    | :? IWildPat -> false
    | _ -> true

let rec compoundPatternNeedsParens (strictContext: ITreeNode) (fsPattern: IFSharpPattern) =
    if isNull strictContext then false else

    match fsPattern with
    | :? IAsPat as asPat ->
        compoundPatternNeedsParens strictContext asPat.LeftPattern ||
        compoundPatternNeedsParens strictContext asPat.RightPattern

    | :? ITuplePat as tuplePat ->
        Seq.exists (compoundPatternNeedsParens strictContext) tuplePat.Patterns

    | :? IAttribPat
    | :? ITypedLikePat -> true

    | _ -> false

let checkPrecedence (context: IFSharpPattern) pat =
    precedence pat < precedence context.Parent

let getBindingPattern (context: IFSharpPattern) =
    let rec loop seenTuple (fsPattern: IFSharpPattern) =
        let tuplePat = TuplePatNavigator.GetByPattern(fsPattern)
        if not seenTuple && isNotNull tuplePat then loop true tuplePat else

        let asPat = AsPatNavigator.GetByLeftPattern(fsPattern)
        if isNotNull asPat then loop seenTuple asPat else

        fsPattern

    loop (context :? ITuplePat) context

let getStrictContext context =
    let bindingPattern = getBindingPattern context
    BindingNavigator.GetByHeadPattern(bindingPattern)

let prefersReferenceResolveRules (refPat: IReferencePat) =
    isNotNull refPat &&

    let referenceName = refPat.ReferenceName
    isNotNull referenceName &&
    
    let name = referenceName.ShortName
    not (name.IsEmpty()) && name[0].IsUpperFast()

let rec needsParens (context: IFSharpPattern) (fsPattern: IFSharpPattern) =
    if isCompoundPattern fsPattern && isNotNull (AsPatNavigator.GetByRightPattern(context)) then true else

    match fsPattern with
    | :? IListConsPat as listConsPat ->
        checkPrecedence context fsPattern ||

        let headPat = listConsPat.HeadPattern
        isNotNull headPat && needsParens context headPat ||

        isNotNull (ListConsPatNavigator.GetByHeadPattern(context))

    | :? IAsPat ->
        isAtCompoundPatternRightSide context ||
        isNotNull (ParametersOwnerPatNavigator.GetByParameter(context)) ||
        isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
        isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context)) ||

        let strictContext = getStrictContext context
        compoundPatternNeedsParens strictContext fsPattern

    | :? ITuplePat as tuplePat ->
        checkPrecedence context fsPattern ||

        isNotNull (TuplePatNavigator.GetByPattern(context)) ||
        isNotNull (AndsPatNavigator.GetByPattern(context)) ||

        // todo: suggest moving parens to a single inner pattern?
        let strictContext = getStrictContext context
        compoundPatternNeedsParens strictContext fsPattern ||

        let matchClause = MatchClauseNavigator.GetByPattern(context)
        isNotNull matchClause && tuplePat.PatternsEnumerable.LastOrDefault() :? ITypedLikePat

    | :? IParametersOwnerPat ->
        checkPrecedence context fsPattern ||

        isNotNull (BindingNavigator.GetByHeadPattern(context)) ||
        isNotNull (ParametersOwnerPatNavigator.GetByParameter(context))

    | :? ITypedLikePat
    | :? IAttribPat ->
        checkPrecedence context fsPattern ||

        isNotNull (BindingNavigator.GetByHeadPattern(getBindingPattern context)) ||
        isNotNull (LambdaParametersListNavigator.GetByPattern(context)) ||
        isNotNull (MatchClauseNavigator.GetByPattern(context)) ||

        let tuplePat = TuplePatNavigator.GetByPattern(context)
        let matchClause = MatchClauseNavigator.GetByPattern(tuplePat)
        isNotNull matchClause && tuplePat.PatternsEnumerable.LastOrDefault() == context

    | :? IWildPat -> false

    | _ ->

    checkPrecedence context fsPattern ||

    let refPat = fsPattern.As<IReferencePat>()
    isNotNull (BindingNavigator.GetByHeadPattern(context)) && prefersReferenceResolveRules refPat ||

    // todo: add code style setting
    let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(context)
    isNotNull parametersOwnerPat && getNextSibling parametersOwnerPat.ReferenceName == context ||

    let parameterDecl = ParametersPatternDeclarationNavigator.GetByPattern(context)
    isNotNull (ConstructorDeclarationNavigator.GetByParametersDeclaration(parameterDecl)) ||

    // todo: add code style setting
    let memberDeclaration = MemberDeclarationNavigator.GetByParametersDeclaration(parameterDecl)
    isNotNull memberDeclaration && getNextSibling memberDeclaration.NameIdentifier == parameterDecl ||
    isNotNull memberDeclaration && getNextSibling memberDeclaration.TypeParameterList == parameterDecl

let escapesTuplePatParamDecl (context: IFSharpPattern) (innerPattern: IFSharpPattern) =
    match innerPattern with
    | :? IParenPat as parenPat ->
        match parenPat.Pattern with
        | :? ITuplePat -> isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(context))
        | _ -> false
    | _ -> false

let addParens (pattern: IFSharpPattern) =
    use writeLockCookie = WriteLockCookie.Create(pattern.IsPhysical())

    let parenPat = box (ElementType.PAREN_PAT.Create()) :?> IParenPat
    SandBox.CreateSandBoxFor(parenPat, pattern.GetPsiModule())

    ModificationUtil.AddChild(parenPat, FSharpTokenType.LPAREN.CreateLeafElement()) |> ignore
    ModificationUtil.AddChild(parenPat, pattern.Copy()) |> ignore
    ModificationUtil.AddChild(parenPat, FSharpTokenType.RPAREN.CreateLeafElement()) |> ignore
    let parenPattern = ModificationUtil.ReplaceChild(pattern, parenPat)
    parenPattern.Pattern

let addParensIfNeeded (pattern: IFSharpPattern) =
    if isNull pattern then pattern else

    let contextPattern = pattern.IgnoreParentParens()
    if contextPattern == pattern && needsParens contextPattern pattern then
        use writeLockCookie = WriteLockCookie.Create(pattern.IsPhysical())

        let pattern = addParens pattern
        let parenPattern = ParenPatNavigator.GetByPattern(pattern)

        let removeSpace (nameNode: ITreeNode) (pattern: ITreeNode) =
            if isNull nameNode then () else

            let nextSibling = nameNode.NextSibling
            if isInlineSpace nextSibling && nextSibling.NextSibling == pattern && nextSibling.GetTextLength() = 1 then
                let settingsStore = pattern.GetSettingsStoreWithEditorConfig()
                let spaceBeforeUppercase =
                    settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeUppercaseInvocation)

                if not spaceBeforeUppercase then
                    ModificationUtil.DeleteChild(nextSibling)

        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(parenPattern)
        if isNotNull parametersOwnerPat then
            removeSpace parametersOwnerPat.ReferenceName parenPattern

        let patternDeclaration = ParametersPatternDeclarationNavigator.GetByPattern(parenPattern)
        let memberDeclaration = MemberDeclarationNavigator.GetByParametersDeclaration(patternDeclaration)
        if isNotNull memberDeclaration then
            removeSpace memberDeclaration.Identifier patternDeclaration

        pattern
    else
        pattern
