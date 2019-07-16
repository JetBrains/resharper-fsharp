namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedSelfIdVariableFix(warning: UnusedThisVariableWarning) =
    inherit QuickFixBase()

    let selfId = warning.SelfId

    override x.Text = "Remove self id"
    override x.IsAvailable _ = isValid selfId

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(selfId.IsPhysical())

        let ctor = ImplicitConstructorDeclarationNavigator.GetBySelfIdentifier(selfId).NotNull()

        // todo: move comments (if any) out of ctor node (see example below) to outer node
        // type T() (* foo *) as this = ...

        match selfId.PrevSibling with
        | Whitespace node -> ModificationUtil.DeleteChildRange(node, selfId)
        | _ -> ModificationUtil.DeleteChild(selfId)

        match ctor.NextSibling with
        | Whitespace node ->
            if node.GetTextLength() <> 1 then
                ModificationUtil.ReplaceChild(node, Whitespace()) |> ignore
        | IsNonNull node ->
            ModificationUtil.AddChildBefore(node, Whitespace()) |> ignore
        | _ -> ()

        null
