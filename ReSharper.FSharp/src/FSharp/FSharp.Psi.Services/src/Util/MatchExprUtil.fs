module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.MatchExprUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util

let isLastClause (clause: IMatchClause) =
    let expr = MatchExprNavigator.GetByClause(clause)
    isNotNull expr && clause == expr.Clauses.Last()

let addIndent (clause: IMatchClause) =
    if not (isLastClause clause) then () else

    let expr = clause.Expression
    if isNull expr || expr.Indent <> clause.Indent then () else

    if expr.IsSingleLine then
        let oldRange = TreeRange(clause.RArrow.NextSibling, expr.PrevSibling)
        ModificationUtil.ReplaceChildRange(oldRange, TreeRange(Whitespace())) |> ignore
    else
        if clause.RArrow.StartLine.Plus1() < expr.StartLine then
            let lineEnding = expr.GetLineEnding()
            let oldRange = TreeRange(clause.RArrow.NextSibling, expr.PrevSibling)
            ModificationUtil.DeleteChildRange(oldRange)
            ModificationUtil.AddChildAfter(clause.RArrow, Whitespace(clause.Indent)) |> ignore
            ModificationUtil.AddChildAfter(clause.RArrow, NewLine(lineEnding)) |> ignore

        let indentSize = expr.GetIndentSize()
        shiftWithWhitespaceBefore indentSize expr

// todo: check 'with'/'function' instead of expr.StartLine
let addBarIfNeeded (expr: IMatchLikeExpr) =
    let firstClause = expr.Clauses.FirstOrDefault()
    if isNotNull firstClause && isNull firstClause.Bar && firstClause.StartLine <> expr.StartLine then
        let bar = FSharpTokenType.BAR.CreateLeafElement()
        addNodeBefore firstClause.FirstChild bar
        firstClause.FormatNode()
