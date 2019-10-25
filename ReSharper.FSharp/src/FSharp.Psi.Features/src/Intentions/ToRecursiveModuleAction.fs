namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ContextAction(Name = "ToRecursiveModule", Group = "F#", Description = "To recursive")>]
type ToRecursiveModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To recursive"

    override x.IsAvailable _ =
        let moduleDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        if isNull moduleDeclaration || moduleDeclaration.IsRecursive then false else

        let moduleToken = moduleDeclaration.ModuleOrNamespaceKeyword
        if isNull moduleToken then false else

        let ranges = DisjointedTreeTextRange.From(moduleToken)
        
        match moduleDeclaration with
        | :? IGlobalNamespaceDeclaration as globalNs -> ranges.Then(globalNs.GlobalKeyword)
        | _ -> ranges.Then(moduleDeclaration.NameIdentifier)
        |> ignore

        ranges.Contains(dataProvider.SelectedTreeRange)

    override x.ExecutePsiTransaction(_, _) =
        let moduleLikeDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        moduleLikeDeclaration.SetIsRecursive(true)

        null
