[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.DataProviders

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

type FSharpContextActionDataProvider(solution, textControl, fsFile) =
    inherit CachedContextActionDataProviderBase<IFSharpFile>(solution, textControl, fsFile)


[<ContextActionDataBuilder(typeof<FSharpContextActionDataProvider>)>]
type FSharpContextActionDataBuilder() =
    inherit ContextActionDataBuilderBase<FSharpLanguage, IFSharpFile>()

    override x.BuildFromPsi(solution, textControl, fsFile) =
        FSharpContextActionDataProvider(solution, textControl, fsFile) :> _


let isAtModuleDeclarationKeyword (dataProvider: IContextActionDataProvider) (declaration: IDeclaredModuleLikeDeclaration) =
    if isNull declaration then false else

    let moduleToken = declaration.ModuleOrNamespaceKeyword
    if isNull moduleToken then false else

    let ranges = DisjointedTreeTextRange.From(moduleToken)

    match declaration with
    | :? IGlobalNamespaceDeclaration as globalNs -> ranges.Then(globalNs.GlobalKeyword)
    | _ -> ranges.Then(declaration.NameIdentifier)
    |> ignore

    ranges.Contains(dataProvider.SelectedTreeRange)

let isAtIfExprKeyword (dataProvider: IContextActionDataProvider) (ifExpr: IIfThenElseExpr) =
    if isNull ifExpr then false else

    let ifKeyword = ifExpr.IfKeyword
    if isNull ifKeyword then false else

    let thenKeyword = ifExpr.ThenKeyword
    if isNull thenKeyword then false else

    let ranges = DisjointedTreeTextRange.From(ifKeyword)
    ranges.Then(thenKeyword) |> ignore

    let elseKeyword = ifExpr.ElseKeyword
    if isNotNull elseKeyword then
        ranges.Then(elseKeyword) |> ignore

    ranges.Contains(dataProvider.SelectedTreeRange)

let isAtTreeNode (dataProvider: IContextActionDataProvider) (node: ITreeNode) =
    isNotNull node && DisjointedTreeTextRange.From(node).Contains(dataProvider.SelectedTreeRange)

let isAtParametersOwnerKeywordOrIdentifier (dataProvider: IContextActionDataProvider) (owner: IParameterOwnerMemberDeclaration) =
    if isNull owner then false else

    let keyword, identifier =
        match owner with
        | :? IBinding as binding ->
            let keyword = binding.BindingKeyword
            let refPat = binding.HeadPattern.IgnoreInnerParens().As<IReferencePat>()
            let identifier = if isNull refPat then null else refPat.Identifier
            keyword, identifier

        | :? IMemberDeclaration as memberDeclaration ->
            memberDeclaration.MemberKeyword, memberDeclaration.Identifier.As<IFSharpIdentifier>()
        | _ ->
            null, null

    if isNull keyword || isNull identifier then false else

    let ranges = DisjointedTreeTextRange.From(keyword)
    ranges.Then(identifier) |> ignore
    ranges.Contains(dataProvider.SelectedTreeRange)

let isAtLetExprKeywordOrReferencePattern (dataProvider: IContextActionDataProvider) (letBindings: ILetBindings) =
    if isNull letBindings then false else

    let letToken = letBindings.BindingKeyword
    if isNull letToken then false else

    let ranges = DisjointedTreeTextRange.From(letToken)

    let bindings = letBindings.BindingsEnumerable
    if bindings.IsEmpty() then false else

    match bindings.FirstOrDefault().HeadPattern.As<IReferencePat>() with
    | null -> false
    | parametersOwnerPat ->

    match parametersOwnerPat.Identifier with
    | null -> false
    | identifier ->

    ranges.Then(identifier) |> ignore
    ranges.Contains(dataProvider.SelectedTreeRange)
