namespace JetBrains.ReSharper.Plugins.FSharp.Intentions

open JetBrains.Application.Parts
open JetBrains.Application.UI.Controls.BulbMenu.Items
open JetBrains.ReSharper.Psi.Tree

[<Interface>]
[<DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)>]
type ISpecifyTypeActionProvider =
    abstract member TryCreateSpecifyTypeAction: node: ITreeNode -> BulbMenuItem
