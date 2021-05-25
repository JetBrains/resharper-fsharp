namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceIfByConditionOperandFix(warning: IfCanBeReplacedByConditionOperandWarning) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr
    let needNegation = warning.NeedNegation

    override this.Text =
        if needNegation then "Replace by condition operand negation"
        else "Replace by condition operand"

    override this.IsAvailable _ = isValid expr

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let newExpr =
            if needNegation then createLogicallyNegatedExpression expr.ConditionExpr else expr.ConditionExpr

        let ifExprIndent = expr.Indent
        let newExprIndent = newExpr.Indent
        let indentDiff = ifExprIndent - newExprIndent

        let expr = ModificationUtil.ReplaceChild(expr, newExpr.Copy())
        shiftNode indentDiff expr
