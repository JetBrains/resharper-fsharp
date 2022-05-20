[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.DataProviders

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
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

let isAtMemberDeclaration (dataProvider: IContextActionDataProvider) (binding: IMemberDeclaration) =
    if isNull binding then false else
    let ranges = DisjointedTreeTextRange.From(binding.MemberKeyword).Then(binding.Identifier)
    ranges.Contains(dataProvider.SelectedTreeRange)

let isAtBindingKeywordOrReferencePatternOrGenericParameters (dataProvider: IContextActionDataProvider) (binding: IBinding) =
    if isNull binding then false else

    let letToken = binding.BindingKeyword
    if isNull letToken then false else

    let ranges = DisjointedTreeTextRange.From(letToken)

    match binding.HeadPattern.As<IReferencePat>() with
    | null -> false
    | parametersOwnerPat ->

    match parametersOwnerPat.Identifier with
    | null -> false
    | identifier ->

    ranges.Then(identifier) |> ignore

    match binding.HeadPattern.GetNextMeaningfulSibling() with
    | :? IPostfixTypeParameterDeclarationList as list ->
        ranges.Then(list) |> ignore
    | _ ->
        ()

    ranges.Contains(dataProvider.SelectedTreeRange)