namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedSelfIdVariableFix(warning: UnusedThisVariableWarning) =
    inherit FSharpQuickFixBase()

    let selfId = warning.SelfId

    override x.Text = "Remove self id"
    override x.IsAvailable _ = isValid selfId

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(selfId.IsPhysical())
        use formatter = FSharpExperimentalFeatures.EnableFormatterCookie.Create()

        // todo: move comments (if any) out of ctor node (see example below) to outer node
        // type T() (* foo *) as this = ...

        deleteChild selfId
