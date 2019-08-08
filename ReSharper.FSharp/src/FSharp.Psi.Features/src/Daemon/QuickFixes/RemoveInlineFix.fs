namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type RemoveInlineFix(error: LocalClassBindingsCannotBeInlineError) =
    inherit QuickFixBase()

    let letModuleDecl = error.LetModuleDecl

    override x.Text = "Remove 'inline'"
    override x.IsAvailable _ =
        isValid letModuleDecl &&
        isNotNull letModuleDecl.InlineKeyword

    override x.ExecutePsiTransaction(_, _) =
        letModuleDecl.SetIsInline(false)
        null
