namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

[<RequireQualifiedAccess>]
type GeneratedClauseExpr =
    | Todo
    | ArgumentOutOfRange


type AddMatchAllClauseFix(expr: IMatchLikeExpr, generatedExpr: GeneratedClauseExpr) =
    inherit FSharpQuickFixBase()

    new (warning: MatchIncompleteWarning) =
        AddMatchAllClauseFix(warning.Expr, GeneratedClauseExpr.Todo)

    new (warning: EnumMatchIncompleteWarning) =
        AddMatchAllClauseFix(warning.Expr, GeneratedClauseExpr.ArgumentOutOfRange)

    override x.Text = "Add '_' pattern"

    override x.IsAvailable _ =
        isValid expr && not expr.Clauses.IsEmpty

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()

        MatchExprUtil.addBarIfNeeded expr
        moveCommentsAndWhitespaceInside expr

        let clause = ModificationUtil.AddChild(expr, factory.CreateMatchClause())

        if generatedExpr = GeneratedClauseExpr.ArgumentOutOfRange then
            let exnTypeElement = expr.GetPredefinedType().ArgumentOutOfRangeException.GetTypeElement()
            let bodyExpr = factory.CreateExpr($"{exnTypeElement.ShortName}() |> raise")
            let binaryAppExpr = clause.SetExpression(bodyExpr) :?> IBinaryAppExpr
            let ctorExpr = binaryAppExpr.LeftArgument :?> IPrefixAppExpr
            let refExpr = ctorExpr.FunctionExpression :?> IReferenceExpr
            FSharpBindUtil.bindDeclaredElementToReference expr refExpr.Reference exnTypeElement ""

        Action<_>(fun textControl ->
            let range = clause.Expression.GetDocumentRange()
            textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(range))
