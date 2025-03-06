namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type RemoveRedundantAttributeSuffixFix(warning: RedundantAttributeSuffixWarning) =
    inherit FSharpScopedQuickFixBase(warning.Attribute)

    let attribute = warning.Attribute

    override x.Text = "Remove redundant attribute suffix"
    override x.IsAvailable _ = isValid attribute && isNotNull attribute.ReferenceName

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(attribute.IsPhysical())

        let shortAttributeName = attribute.ReferenceName.ShortName.TrimFromEnd("Attribute")
        attribute.ReferenceName.SetName(shortAttributeName) |> ignore
