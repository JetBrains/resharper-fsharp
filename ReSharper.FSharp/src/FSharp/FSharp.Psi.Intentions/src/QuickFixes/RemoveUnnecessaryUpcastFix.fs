namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell

type RemoveUnnecessaryUpcastFix(warning: UpcastUnnecessaryWarning) =
    inherit FSharpQuickFixBase()

    let upcastExpr = warning.UpcastExpr

    override x.Text = "Remove upcast"

    override x.IsAvailable _ =
        isValid upcastExpr && isValid upcastExpr.Expression

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(upcastExpr.IsPhysical())

        replaceWithCopy upcastExpr upcastExpr.Expression
