﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantAttributeFix(attr: IAttribute) =
    inherit FSharpQuickFixBase()

    new (warning: RedundantRequireQualifiedAccessAttributeWarning) =
        RemoveRedundantAttributeFix(warning.Attr)

    new (error: AttributeIsNotValidOnThisElementError) =
        RemoveRedundantAttributeFix(error.Attribute)

    new (warning: ExtensionAttributeIsRedundantWarning) =
        RemoveRedundantAttributeFix(warning.Attribute)

    override x.Text = "Remove attribute"

    override x.IsAvailable _ =
        isValid attr && isNotNull (AttributeListNavigator.GetByAttribute(attr))

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(attr.IsPhysical())
        FSharpAttributesUtil.removeAttributeOrList attr
