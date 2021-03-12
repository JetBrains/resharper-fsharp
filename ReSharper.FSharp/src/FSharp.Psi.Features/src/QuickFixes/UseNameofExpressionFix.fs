namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type UseNameofExpressionFix(warning: UseNameofExpressionWarning) =
    inherit FSharpQuickFixBase()

    let literalExpr = warning.LiteralExpr

    override this.Text = "Use 'nameof' expression"

    override this.IsAvailable _ =
        isValid literalExpr

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(literalExpr.IsPhysical())

        let factory = literalExpr.CreateElementFactory()
        let newExpr = factory.CreateExpr($"nameof {warning.Reference.GetName()}")
        ModificationUtil.ReplaceChild(literalExpr, newExpr) |> addParensIfNeeded |> ignore


