namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "InvertIf", Group = "F#", Description = "Invert 'if' expression")>]
type InvertIfAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "Invert 'if'"

    override x.IsAvailable _ =
        let ifExpr = dataProvider.GetSelectedElement<IIfThenElseExpr>()
        if isNull ifExpr then false else

        let elseExpr = ifExpr.ElseExpr
        if isNull elseExpr || elseExpr :? IElifExpr then false else

        // todo: remove
        if not ifExpr.IsSingleLine then false else

        isAtIfExprKeyword dataProvider ifExpr

    override x.ExecutePsiTransaction(_, _) =
        let ifExpr = dataProvider.GetSelectedElement<IIfThenElseExpr>()
        use writeCookie = WriteLockCookie.Create(ifExpr.IsPhysical())

        let conditionExpr = ifExpr.ConditionExpr
        let negatedExpression = createLogicallyNegatedExpression conditionExpr
        let replaced = ModificationUtil.ReplaceChild(conditionExpr, negatedExpression)
        addParensIfNeeded replaced |> ignore

        let oldThenExpr = ifExpr.ThenExpr
        let thenExpr = oldThenExpr.Copy()

        replaceWithCopy oldThenExpr ifExpr.ElseExpr
        replace ifExpr.ElseExpr thenExpr

        null
