namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

type ConvertToUseFix(warning: ConvertToUseBindingWarning) =
    inherit FSharpQuickFixBase()

    let letExpr = warning.LetExpr

    override x.Text = "Convert to 'use' binding"

    override x.IsAvailable _ =
        isValid letExpr && isValid letExpr.BindingKeyword

    override x.ExecutePsiTransaction _ =
        LetToUseAction.Execute(letExpr)
