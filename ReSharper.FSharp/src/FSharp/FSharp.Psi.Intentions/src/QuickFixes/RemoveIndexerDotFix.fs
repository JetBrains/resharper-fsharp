namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveIndexerDotFix(warning: RedundantIndexerDotWarning) =
    inherit FSharpScopedQuickFixBase(warning.IndexerExpr)

    let indexerExpr = warning.IndexerExpr

    override x.Text = "Remove redundant '.'"

    override this.IsAvailable _ = 
        isValid indexerExpr

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(indexerExpr.IsPhysical())

        ModificationUtil.DeleteChildRange(indexerExpr.Qualifier.NextSibling, indexerExpr.IndexerArgList.PrevSibling)
        replaceWithNodeKeepChildren indexerExpr.IndexerArgList ElementType.LIST_EXPR |> ignore
        replaceWithNodeKeepChildren indexerExpr ElementType.PREFIX_APP_EXPR |> ignore
