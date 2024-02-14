namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.ObjExprUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("with", "Create object expression", "{ new T with }")>]
type NewObjPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override x.CreateBehavior(info) = NewObjPostfixTemplateBehavior(info) :> _
    override x.CreateInfo(context) = NewObjPostfixTemplateInfo(context) :> _

    override this.IsApplicable(node) =
        let refExpr = node.As<IReferenceExpr>()
        isNotNull refExpr &&

        let expr = node.As<IFSharpExpression>()
        FSharpPostfixTemplates.canBecomeStatement false expr &&

        NewObjPostfixTemplate.isApplicableExpr refExpr.Qualifier

    override this.IsEnabled _ = true

and NewObjPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("with", expressionContext)

and NewObjPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override this.ExpandPostfix(context) =
        let node = context.Expression
        let psiModule = node.GetPsiModule()

        psiModule.GetPsiServices().Transactions.Execute(this.ExpandCommandName, fun _ ->
            let factory = node.CreateElementFactory()

            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let expr = this.GetExpression(context)
            GenerateOverrides.convertToObjectExpression factory psiModule expr
        )

    override this.AfterComplete(textControl, node, _) =
        let objExpr = node :?> IObjExpr
        GenerateOverrides.selectObjExprMemberOrCallCompletion objExpr textControl
