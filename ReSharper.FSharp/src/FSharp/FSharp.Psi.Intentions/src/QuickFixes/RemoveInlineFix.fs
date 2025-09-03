namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI

type RemoveInlineFix(error: LocalClassBindingsCannotBeInlineError) =
    inherit FSharpQuickFixBase()

    let binding = error.Binding

    override x.Text = "Remove 'inline'"

    override x.IsAvailable _ =
        isValid binding && binding.IsInline

    override x.ExecutePsiTransaction _ =
        binding.SetIsInline(false)
