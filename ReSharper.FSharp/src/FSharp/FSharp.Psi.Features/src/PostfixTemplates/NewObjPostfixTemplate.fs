namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

module NewObjPostfixTemplate =
    let private isApplicableArgExpr (expr: IFSharpExpression) =
        expr :? IParenExpr || expr :? IUnitExpr

    let getReference (expr: IFSharpExpression) =
        match expr with
        | :? IReferenceExpr as refExpr -> refExpr.Reference
        | :? IPrefixAppExpr as appExpr ->
            match appExpr.FunctionExpression with
            | :? IReferenceExpr as refExpr when isApplicableArgExpr appExpr.ArgumentExpression -> refExpr.Reference
            | _ -> null
        | _ -> null

    let getTypeElement (reference: IReference) =
        if isNull reference then null else

        match reference.Resolve().DeclaredElement with
        | :? ITypeElement as typeElement -> typeElement
        | :? IConstructor as ctor -> ctor.ContainingType
        | _ -> null

    let createObjExpr (factory: IFSharpElementFactory) (expr: IFSharpExpression) =
        let text, argExpr =
            match expr with
            | :? IReferenceExpr as refExpr -> refExpr.GetText(), null
            | :? IPrefixAppExpr as appExpr ->
                match appExpr.FunctionExpression with
                | :? IReferenceExpr as refExpr ->
                    refExpr.GetText(), appExpr.ArgumentExpression
                | _ -> failwith "Unreachable"
            | _ -> failwith "Unreachable"

        let objExpr = factory.CreateExpr($"{{ new {text} with }}") :?> IObjExpr
        if isNotNull argExpr then
            ModificationUtil.AddChildAfter(objExpr.TypeName, argExpr.Copy()) |> ignore
        objExpr

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

        let reference = NewObjPostfixTemplate.getReference refExpr.Qualifier
        let typeElement = NewObjPostfixTemplate.getTypeElement reference
        (typeElement :? IClass || typeElement :? IInterface) && not (typeElement.IsRecord() || typeElement.IsUnion())

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
            let reference = NewObjPostfixTemplate.getReference expr
            let fcsSymbolUse = reference.GetSymbolUse()

            let objExpr = NewObjPostfixTemplate.createObjExpr factory expr
            let inputElements = GenerateOverrides.getOverridableMembersForType null fcsSymbolUse true true psiModule
            let indent = expr.Indent + expr.GetIndentSize()
            GenerateOverrides.addMembers inputElements objExpr indent objExpr.WithKeyword |> ignore
            ModificationUtil.ReplaceChild(expr, objExpr)
        )

    override this.AfterComplete(textControl, node, _) =
        let objExpr = node :?> IObjExpr
        objExpr.MemberDeclarationsEnumerable
        |> Seq.tryHead
        |> Option.iter (fun decl ->
            let memberDecl = decl.As<IMemberDeclaration>()
            if isNull memberDecl then () else

            let expr = memberDecl.Expression
            textControl.Caret.MoveTo(expr.GetDocumentEndOffset(), CaretVisualPlacement.DontScrollIfVisible)
            textControl.Selection.SetRange(expr.GetDocumentRange())
        )
