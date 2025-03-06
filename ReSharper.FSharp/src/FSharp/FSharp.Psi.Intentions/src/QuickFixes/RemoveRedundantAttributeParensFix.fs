namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantAttributeParensFix(warning: RedundantAttributeParensWarning) =
    inherit FSharpScopedQuickFixBase(warning.Attribute)

    let attribute = warning.Attribute

    override x.Text = "Remove redundant parentheses"
    override x.IsAvailable _ = isValid attribute && isNotNull attribute.ArgExpression

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(attribute.IsPhysical())

        deleteChild attribute.ArgExpression
