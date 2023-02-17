namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

type FormatterImplHelper = JetBrains.ReSharper.Psi.Impl.CodeStyle.FormatterImplHelper

[<PostfixTemplate("match", "Pattern match expression", "match expr with | _ -> ()")>]
type MatchPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override x.CreateBehavior(info) = MatchPostfixTemplateBehavior(info) :> _

    override x.TryCreateInfo(context) =
        let context = context.AllExpressions[0]
        let expr = context.Expression.As<IReferenceExpr>()
        if isNull expr then null else

        let expr =
            expr
            |> FSharpPostfixTemplates.getContainingArgExpr
            |> FSharpPostfixTemplates.getContainingTupleExpr

        if FSharpPostfixTemplates.canBecomeStatement expr && FSharpIntroduceVariable.CanIntroduceVar(expr, true) then
            MatchPostfixTemplateInfo(context) :> _
        else
            null


and MatchPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("match", expressionContext)


and MatchPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiServices = context.PostfixContext.PsiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let expr = x.GetExpression(context)
            let expr = FSharpPostfixTemplates.getContainingTupleExpr expr

            let contextExpr: ITreeNode = 
                match ChameleonExpressionNavigator.GetByExpression(expr) with
                | null -> expr
                | chameleon -> chameleon

            if not (isFirstMeaningfulNodeOnLine contextExpr) then
                let contextIndent = 
                    let matchClause = MatchClauseNavigator.GetByExpression(expr)
                    let tryFinallyExpr = TryWithExprNavigator.GetByClause(matchClause)
                    if isNotNull tryFinallyExpr && matchClause.StartLine = tryFinallyExpr.WithKeyword.StartLine then
                        tryFinallyExpr.WithKeyword.Indent else

                    let lambdaExpr = LambdaExprNavigator.GetByExpression(expr)
                    if isNotNull lambdaExpr then
                        let formatter = lambdaExpr.Language.LanguageServiceNotNull().CodeFormatter
                        FormatterImplHelper.CalcLineIndent(lambdaExpr, formatter).Length else

                    contextExpr.Parent.Indent

                addNodesBefore contextExpr [
                    NewLine(expr.GetLineEnding())
                    Whitespace(contextIndent + expr.GetIndentSize())
                ] |> ignore

            let matchExpr = ModificationUtil.ReplaceChild(expr, expr.CreateElementFactory().CreateMatchExpr(expr))
            let matchClause = matchExpr.Clauses[0]
            ModificationUtil.DeleteChildRange(matchClause.Pattern, matchClause.LastChild)

            matchExpr :> ITreeNode
        )

    override x.AfterComplete(textControl, node, _) =
        textControl.Caret.MoveTo(node.GetDocumentEndOffset() + 1, CaretVisualPlacement.DontScrollIfVisible)
        textControl.RescheduleCompletion(node.GetSolution())
