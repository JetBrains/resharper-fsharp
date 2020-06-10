namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type RemoveRedundantAttributeSuffixFix(warning: RedundantAttributeSuffixWarning) =
    inherit FSharpScopedQuickFixBase()

    let attribute = warning.Attribute

    override x.Text = "Remove redundant attribute suffix"
    override x.IsAvailable _ = isValid attribute && isNotNull attribute.ReferenceName

    override x.TryGetContextTreeNode() = attribute :> _

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(attribute.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let shortAttributeName = attribute.ReferenceName.ShortName.TrimFromEnd("Attribute")
        attribute.ReferenceName.SetName(shortAttributeName) |> ignore
