namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type NegateConditionActionBase<'T when 'T: null and 'T :> IConditionOwnerExpr>(dataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    abstract GetExpression: unit -> 'T
    abstract GetKeyword: 'T -> ITokenNode

    override x.IsAvailable _ =
        let expr = x.GetExpression()
        if isNull expr then false else

        let keyword = x.GetKeyword(expr)
        x.IsAtTreeNode(keyword)

    override x.ExecutePsiTransaction(_, _) =
        let expr = x.GetExpression().ConditionExpr
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let negatedExpr = createLogicallyNegatedExpression expr
        let replacedExpr = ModificationUtil.ReplaceChild(expr, negatedExpr)
        replacedExpr |> addParensIfNeeded |> ignore

        null


[<ContextAction(Name = "NegateIfCondition", Group = "F#", Description = "Negate 'if' condition")>]
type NegateIfConditionAction(dataProvider: FSharpContextActionDataProvider) =
    inherit NegateConditionActionBase<IIfThenElseExpr>(dataProvider)

    override x.Text = "Negate 'if' condition"

    override x.GetExpression() = dataProvider.GetSelectedElement<IIfThenElseExpr>()
    override x.GetKeyword(expr) = expr.IfKeyword


[<ContextAction(Name = "NegateWhileCondition", Group = "F#", Description = "Negate 'while' condition")>]
type NegateWhileConditionAction(dataProvider: FSharpContextActionDataProvider) =
    inherit NegateConditionActionBase<IWhileExpr>(dataProvider)

    override x.Text = "Negate 'while' condition"

    override x.GetExpression() = dataProvider.GetSelectedElement<IWhileExpr>()
    override x.GetKeyword(expr) = expr.WhileKeyword
