namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

type ReplaceAppExprWithArgFix(warning: RedundantApplicationWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.AppExpr, warning.ArgExpr, false)

    let funExpr = getFunctionExpr warning.AppExpr

    override x.Text =
        $"Remove %s{getExprPresentableName funExpr}"

    override x.IsAvailable(dataHolder) =
        base.IsAvailable(dataHolder) && isValid funExpr
