namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantAttributeFix(warning: RedundantRequireQualifiedAccessAttributeWarning) =
    inherit FSharpQuickFixBase()

    let attr = warning.Attr

    override x.Text = "Remove redundant attribute"

    override x.IsAvailable _ =
        isValid attr && isNotNull (AttributeListNavigator.GetByAttribute(attr)) 

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(attr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        FSharpAttributesUtil.removeAttributeOrList attr
