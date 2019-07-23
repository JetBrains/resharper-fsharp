namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "ToMultilineRecord", Group = "F#", Description = "Converts record expression to multiline")>]
type ToMultilineRecord(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To multiline"

    override x.IsAvailable _ =
        let recordExpr = dataProvider.GetSelectedElement<IRecordExpr>()
        if isNull recordExpr then false else
        if isNotNull recordExpr.CopyInfoExpression || recordExpr.ExprBindings.Count < 2 then false else
        if not recordExpr.IsSingleLine then false else

        let lBrace = recordExpr.LeftBrace
        let rBrace = recordExpr.RightBrace
        if isNull lBrace || isNull rBrace then false else

        let ranges = DisjointedTreeTextRange.From(lBrace).Then(rBrace)
        ranges.Contains(dataProvider.SelectedTreeRange)

    override x.ExecutePsiTransaction(_, _) =
        let recordExpr = dataProvider.GetSelectedElement<IRecordExpr>()
        let lineEnding = recordExpr.FSharpFile.GetLineEnding()

        use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())

        let bindings = recordExpr.ExprBindings
        let firstBinding = bindings.[0]
        let indent = firstBinding.Indent

        for binding in bindings do
            if binding != firstBinding then
                let newLine =
                    match binding.PrevSibling with
                    | Whitespace node -> ModificationUtil.ReplaceChild(node, NewLine(lineEnding))
                    | node -> ModificationUtil.AddChildAfter(node, NewLine(lineEnding))

                ModificationUtil.AddChildAfter(newLine, Whitespace(indent)) |> ignore

            match binding.Semicolon with
            | null -> ()
            | semicolon ->

             // todo: move comments out of bindings
            match semicolon.PrevSibling with
            | Whitespace node -> ModificationUtil.DeleteChildRange(node, semicolon)
            | _ -> ModificationUtil.DeleteChild(semicolon)

        null
