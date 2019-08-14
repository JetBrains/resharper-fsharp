namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type RemoveUnnecessaryUpcastFix(warning: UpcastUnnecessaryWarning) =
    inherit QuickFixBase()

    let upcastExpr = warning.UpcastExpr

    override x.Text = "Remove upcast"

    override x.IsAvailable _ =
        isValid upcastExpr && isValid upcastExpr.Expression

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(upcastExpr.IsPhysical())
        replaceWithCopy upcastExpr upcastExpr.Expression
        null
