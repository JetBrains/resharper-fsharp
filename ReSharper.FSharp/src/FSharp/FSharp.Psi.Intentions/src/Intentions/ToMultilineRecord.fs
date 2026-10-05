namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Util

[<ContextAction(Name = "ToMultilineRecord", GroupType = typeof<FSharpContextActions>, Description = "Converts record expression to multiline")>]
type ToMultilineRecord(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To multiline"

    override x.IsAvailable _ =
        let recordExpr = dataProvider.GetSelectedElement<IRecordExpr>()
        if isNull recordExpr then false else
        if isNotNull recordExpr.CopyInfoExpression || recordExpr.Members.Count < 2 then false else
        if not recordExpr.IsSingleLine then false else

        let lBrace = recordExpr.LeftBrace
        let rBrace = recordExpr.RightBrace
        if isNull lBrace || isNull rBrace then false else

        let ranges = DisjointedTreeTextRange.From(lBrace).Then(rBrace)
        ranges.Contains(dataProvider.SelectedTreeRange)

    override x.ExecutePsiTransaction(_, _) =
        RecordExprUtil.toMultiline (dataProvider.GetSelectedElement<IRecordExpr>())
        null
