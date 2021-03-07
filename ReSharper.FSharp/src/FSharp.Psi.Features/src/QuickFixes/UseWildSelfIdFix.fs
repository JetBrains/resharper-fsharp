namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type UseWildSelfIdFix(warning: UseWildSelfIdWarning) =
    inherit FSharpScopedQuickFixBase()

    let selfId = warning.SelfId

    override this.Text = "Replace with '_'"

    override this.IsAvailable _ =
        isValid selfId

    override this.TryGetContextTreeNode() = selfId :> _

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(selfId.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = selfId.CreateElementFactory()
        ModificationUtil.ReplaceChild(selfId, factory.CreateWildSelfId()) |> ignore
