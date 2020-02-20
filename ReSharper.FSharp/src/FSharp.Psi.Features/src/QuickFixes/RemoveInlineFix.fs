namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI

type RemoveInlineFix(error: LocalClassBindingsCannotBeInlineError) =
    inherit FSharpQuickFixBase()

    let letModuleDecl = error.LetModuleDecl

    override x.Text = "Remove 'inline'"
    override x.IsAvailable _ =
        isValid letModuleDecl &&
        isNotNull letModuleDecl.InlineKeyword

    override x.ExecutePsiTransaction _ =
        use disableFormatter = new DisableCodeFormatter()
        letModuleDecl.SetIsInline(false)
