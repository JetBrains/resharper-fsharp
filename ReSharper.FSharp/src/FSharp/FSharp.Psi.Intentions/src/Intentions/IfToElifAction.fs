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
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.Util

[<ContextAction(Name = "IfToElif", Group = "F#", Description = "Converts `if` expression to 'elif'")>]
type IfToElifAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To 'elif'"

    override x.IsAvailable _ =
        let ifExpr = dataProvider.GetSelectedElement<IIfExpr>()
        if not (isValid ifExpr) then false else

        if x.IsAtTreeNode(ifExpr.ElseKeyword) && ifExpr.ElseExpr :? IIfThenElseExpr then true else

        let ifExpr = ifExpr.As<IIfThenElseExpr>()
        let outerIfExpr = IfExprNavigator.GetByElseExpr(ifExpr)
        if isNull ifExpr || isNull outerIfExpr then false else

        isNotNull outerIfExpr.ElseKeyword && x.IsAtTreeNode(ifExpr.IfKeyword)

    override x.ExecutePsiTransaction(_, _) =
        let ifExpr =
            let ifExpr = dataProvider.GetSelectedElement<IIfExpr>()
            if x.IsAtTreeNode(ifExpr.ElseKeyword) then ifExpr.ElseExpr :?> IIfThenElseExpr else ifExpr :?> _

        use writeCookie = WriteLockCookie.Create(ifExpr.IsPhysical())

        replaceWithToken ifExpr.IfKeyword FSharpTokenType.ELIF

        let elseKeyword = IfExprNavigator.GetByElseExpr(ifExpr).ElseKeyword
        let last = getLastMatchingNodeAfter isInlineSpaceOrComment elseKeyword
        ModificationUtil.DeleteChildRange(elseKeyword, last)

        let elifExpr = ModificationUtil.ReplaceChild(ifExpr, ElementType.ELIF_EXPR.Create())
        LowLevelModificationUtil.AddChild(elifExpr, ifExpr.Children().AsArray())

        Action<_>(fun textControl ->
            textControl.Caret.MoveTo(elifExpr.GetDocumentStartOffset(), CaretVisualPlacement.DontScrollIfVisible))
