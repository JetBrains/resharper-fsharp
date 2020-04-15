namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type AddMatchAllClauseFix(expr: IMatchExpr) =
    inherit FSharpQuickFixBase()

    new (warning: MatchIncompleteWarning) =
        AddMatchAllClauseFix(warning.Expr)

    new (warning: EnumMatchIncompleteWarning) =
        AddMatchAllClauseFix(warning.Expr)

    override x.Text = "Add '_' pattern"

    override x.IsAvailable _ =
        isValid expr && not expr.Clauses.IsEmpty

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let clauses = expr.Clauses
        let isSingleLineMatch = expr.IsSingleLine

        let addToNewLine = not isSingleLineMatch // todo: cover more cases
        let indent = if addToNewLine then clauses.Last().Indent else expr.Indent

        addNodesAfter expr.LastChild [
            if addToNewLine then
                NewLine(expr.GetLineEnding())
                if indent > 0 then
                    Whitespace(indent)
            else
                Whitespace()
            expr.CreateElementFactory().CreateMatchClause()
        ] |> ignore
