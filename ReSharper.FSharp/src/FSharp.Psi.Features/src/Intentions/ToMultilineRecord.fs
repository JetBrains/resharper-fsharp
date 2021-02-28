namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "ToMultilineRecord", Group = "F#", Description = "Converts record expression to multiline")>]
type ToMultilineRecord(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To multiline"

    override x.IsAvailable _ =
        let recordExpr = dataProvider.GetSelectedElement<IRecordExpr>()
        if isNull recordExpr then false else
        if isNotNull recordExpr.CopyInfoExpression || recordExpr.FieldBindings.Count < 2 then false else
        if not recordExpr.IsSingleLine then false else

        let lBrace = recordExpr.LeftBrace
        let rBrace = recordExpr.RightBrace
        if isNull lBrace || isNull rBrace then false else

        let ranges = DisjointedTreeTextRange.From(lBrace).Then(rBrace)
        ranges.Contains(dataProvider.SelectedTreeRange)

    override x.ExecutePsiTransaction(_, _) =
        ToMultilineRecord.Execute(dataProvider.GetSelectedElement<IRecordExpr>())
        null

    static member Execute(recordExpr: IRecordExpr) =
        let lineEnding = recordExpr.FSharpFile.GetLineEnding()

        use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())
        use enableFormatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()

        let bindings = recordExpr.FieldBindings
        let firstBinding = bindings.[0]

        for binding in bindings do
            if binding != firstBinding then
                match binding.PrevSibling with
                | Whitespace node -> ModificationUtil.ReplaceChild(node, NewLine(lineEnding)) |> ignore
                | node -> ModificationUtil.AddChildAfter(node, NewLine(lineEnding)) |> ignore

            match binding.Semicolon with
            | null -> ()
            | semicolon -> ModificationUtil.DeleteChild(semicolon)
