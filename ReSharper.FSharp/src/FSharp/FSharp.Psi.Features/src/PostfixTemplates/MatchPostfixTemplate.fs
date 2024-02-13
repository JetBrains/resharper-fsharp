namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
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
    override x.CreateInfo(context) = MatchPostfixTemplateInfo(context) :> _

    override this.IsApplicable(node) =
        let expr = node.As<IFSharpExpression>()
        FSharpPostfixTemplates.canBecomeStatement true expr

    override this.IsEnabled _ = true


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
            let expr =
                expr
                |> FSharpPostfixTemplates.getContainingAppExprFromLastArg false
                |> FSharpPostfixTemplates.getContainingTupleExprFromLastItem

            FSharpPostfixTemplates.convertToBlockLikeExpr expr context

            let matchExpr = ModificationUtil.ReplaceChild(expr, expr.CreateElementFactory().CreateMatchExpr(expr))
            let matchClause = matchExpr.Clauses[0]
            ModificationUtil.DeleteChildRange(matchClause.Pattern, matchClause.LastChild)

            matchExpr :> ITreeNode
        )

    override x.AfterComplete(textControl, node, _) =
        textControl.Caret.MoveTo(node.GetDocumentEndOffset() + 1, CaretVisualPlacement.DontScrollIfVisible)
        textControl.RescheduleCompletion(node.GetSolution())
