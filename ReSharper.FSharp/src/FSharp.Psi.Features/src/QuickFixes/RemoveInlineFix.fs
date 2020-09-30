namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI

type RemoveInlineFix(error: LocalClassBindingsCannotBeInlineError) =
    inherit FSharpQuickFixBase()

    let letBindings = error.LetBindings

    override x.Text = "Remove 'inline'"
    override x.IsAvailable _ =
        isValid letBindings &&
        isNotNull letBindings.InlineKeyword

    override x.ExecutePsiTransaction _ =
        use disableFormatter = new DisableCodeFormatter()
        letBindings.SetIsInline(false)
