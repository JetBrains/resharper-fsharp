namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
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
        use formatterCookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()
        use disableFormatter = new DisableCodeFormatter()

        let isSingleLineMatch = expr.IsSingleLine

        let addToNewLine = not isSingleLineMatch // todo: cover more cases

        let singleClause = expr.Clauses.SingleItem
        if isNotNull singleClause && isNull singleClause.Bar && singleClause.StartLine <> expr.StartLine then
            addNodesBefore singleClause.FirstChild [
                FSharpTokenType.BAR.CreateLeafElement()
                Whitespace()
            ] |> ignore

        let clause =
            addNodesAfter expr.LastChild [
                if addToNewLine then
                    let lineEnding = expr.GetLineEnding()

                    let lastClause = expr.Clauses.Last()
                    if not lastClause.IsSingleLine && isAfterEmptyLine lastClause then
                        NewLine(lineEnding)

                    NewLine(lineEnding)
                    Whitespace(lastClause.Indent) // may be wrong in some cases
                else
                    Whitespace()
                factory.CreateMatchClause()
            ] :?> IMatchClause

        if generatedExpr = GeneratedClauseExpr.ArgumentOutOfRange then
            let typeName =
                let typeName = "ArgumentOutOfRangeException"
                match expr.CheckerService.ResolveNameAtLocation(expr, [typeName], false, "AddMatchAllClauseFix") with
                | None -> $"System.{typeName}"
                | Some _ -> typeName

            clause.SetExpression(factory.CreateExpr($"{typeName}() |> raise")) |> ignore

        Action<_>(fun textControl ->
            let range = clause.Expression.GetNavigationRange()
            textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(range))
