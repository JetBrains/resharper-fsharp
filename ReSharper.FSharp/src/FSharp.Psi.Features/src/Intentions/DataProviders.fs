[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.DataProviders

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type FSharpContextActionDataProvider(solution, textControl, fsFile) =
    inherit CachedContextActionDataProviderBase<IFSharpFile>(solution, textControl, fsFile)


[<ContextActionDataBuilder(typeof<FSharpContextActionDataProvider>)>]
type FSharpContextActionDataBuilder() =
    inherit ContextActionDataBuilderBase<FSharpLanguage, IFSharpFile>()

    override x.BuildFromPsi(solution, textControl, fsFile) =
        FSharpContextActionDataProvider(solution, textControl, fsFile) :> _


let isAtModuleDeclaration (dataProvider: IContextActionDataProvider) (declaration: IDeclaredModuleLikeDeclaration) =
    if isNull declaration then false else

    let moduleToken = declaration.ModuleOrNamespaceKeyword
    if isNull moduleToken then false else

    let ranges = DisjointedTreeTextRange.From(moduleToken)
    
    match declaration with
    | :? IGlobalNamespaceDeclaration as globalNs -> ranges.Then(globalNs.GlobalKeyword)
    | _ -> ranges.Then(declaration.NameIdentifier)
    |> ignore

    ranges.Contains(dataProvider.SelectedTreeRange)
