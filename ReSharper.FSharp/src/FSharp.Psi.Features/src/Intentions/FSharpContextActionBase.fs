namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Psi.Tree

[<AbstractClass>]
type FSharpContextActionBase(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    member x.IsAtTreeNode(node: ITreeNode) =
        DataProviders.isAtTreeNode dataProvider node
