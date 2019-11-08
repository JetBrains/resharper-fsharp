namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type AddParensFix(error: SuccessiveArgsShouldBeSpacedOrTupledError) =
    inherit QuickFixBase()

    let expr = error.Expr

    override x.Text = "Add parens"
    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        addParens expr |> ignore
        null
