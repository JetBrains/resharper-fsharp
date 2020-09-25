namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

type ReplaceAppExprWithArgFix(warning: RedundantApplicationWarning) =
    inherit ReplaceWithInnerExpressionFixBase(warning.AppExpr, warning.ArgExpr)

    let appExpr = warning.AppExpr
    let funExpr = getFunctionExpr appExpr

    override x.Text =
        let name = getReferenceExprName funExpr
        sprintf "Remove '%s'" name

    override x.IsAvailable(dataHolder) =
        base.IsAvailable(dataHolder) && isValid funExpr
