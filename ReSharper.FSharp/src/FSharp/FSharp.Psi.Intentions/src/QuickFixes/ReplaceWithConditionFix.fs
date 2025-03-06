namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithConditionFix(warning: ExpressionCanBeReplacedWithConditionWarning) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr
    let needsNegation = warning.NeedsNegation

    override this.Text =
        if needsNegation then "Replace with condition negation"
        else "Replace with condition"

    override this.IsAvailable _ = isValid expr

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let newExpr =
            if needsNegation then createLogicallyNegatedExpression expr.ConditionExpr else expr.ConditionExpr

        ModificationUtil.ReplaceChild(expr, newExpr.Copy()) |> ignore
