namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.Util

[<ContextAction(Name = "ElifToIf", Group = "F#", Description = "Converts `elif` expression to 'if'")>]
type ElifToIfAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To 'if'"

    override x.IsAvailable _ =
        let elifExpr = dataProvider.GetSelectedElement<IElifExpr>()
        if isNull elifExpr then false else

        let elifKeyword = elifExpr.ElifKeyword
        if not (DisjointedTreeTextRange.From(elifKeyword).Contains(dataProvider.SelectedTreeRange)) then false else

        let ifExpr = IfThenElseExprNavigator.GetByElseExpr(elifExpr)
        isValid ifExpr && isValid ifExpr.ThenExpr

    override x.ExecutePsiTransaction(_, _) =
        let elifExpr = dataProvider.GetSelectedElement<IElifExpr>()
        use writeCookie = WriteLockCookie.Create(elifExpr.IsPhysical())

        replaceWithToken elifExpr.ElifKeyword FSharpTokenType.IF

        let ifExpr = IfExprNavigator.GetByElseExpr(elifExpr)
        let thenExpr = ifExpr.ThenExpr

        addNodesAfter thenExpr [
            Whitespace()
            FSharpTokenType.ELSE.CreateLeafElement()

            if isNotNull thenExpr.NextSibling && not (isWhitespace thenExpr.NextSibling) then
                Whitespace()
        ] |> ignore

        let ifExpr = ModificationUtil.ReplaceChild(elifExpr, ElementType.IF_THEN_ELSE_EXPR.Create())
        LowLevelModificationUtil.AddChild(ifExpr, elifExpr.Children().AsArray())

        Action<_>(fun textControl ->
            textControl.Caret.MoveTo(ifExpr.GetDocumentStartOffset(), CaretVisualPlacement.DontScrollIfVisible))
