namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Psi.Tree

[<AbstractClass>]
type FSharpContextActionBase(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null

    member x.IsAtTreeNode(node: ITreeNode) =
        DataProviders.isAtTreeNode dataProvider node
