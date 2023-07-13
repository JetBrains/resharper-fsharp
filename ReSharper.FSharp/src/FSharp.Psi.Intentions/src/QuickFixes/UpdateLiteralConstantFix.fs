namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type UpdateLiteralConstantFix(error: LiteralConstantValuesDifferError) =
    inherit FSharpQuickFixBase()
    let errorRefPat = error.Pat.As<IReferencePat>()
    let implNeedsLiteralAttr =
        errorRefPat.Attributes
        |> Seq.exists (fun attr ->
            let referenceName = attr.ReferenceName
            isNotNull referenceName && referenceName.ShortName = "Literal")
        |> not
    
    let tryFindDeclarationFromSignature () =
        let containingTypeDecl = errorRefPat.GetContainingTypeDeclaration()
        let decls = containingTypeDecl.DeclaredElement.GetDeclarations()
        decls |> Seq.tryFind (fun d -> d.GetSourceFile().IsFSharpSignatureFile)
        
    let tryFindSigBindingSignature sigMembers =
        let p = errorRefPat.Binding.HeadPattern.As<IFSharpPattern>()
        if Seq.length p.Declarations = 1 then
            let implDec = Seq.head p.Declarations
            let declName = implDec.DeclaredName
            sigMembers
            |>  Seq.tryPick(fun m ->
                let bindingSignature = m.As<IBindingSignature>()
                match bindingSignature with
                | null -> None
                | _ ->
                    match bindingSignature.HeadPattern with
                    | :? IReferencePat as sigRefPat when
                        declName = sigRefPat.DeclaredName -> Some bindingSignature
                    | _ -> None
                )
        else
            None

    let mutable sigRefPat = null

    let rec isImplExprValidInSig (implExpression: IFSharpExpression) =
        match implExpression with
        | :? IReferenceExpr as refExpr ->
            refExpr.Reference.ResolveWithFcs(sigRefPat, System.String.Empty, false, refExpr.IsQualified)
            |> Option.isSome
        | :? IBinaryAppExpr as binExpr ->
            isImplExprValidInSig binExpr.LeftArgument && isImplExprValidInSig binExpr.RightArgument
        | _ -> true

    override x.Text =
        if implNeedsLiteralAttr then $"Add Literal attribute to constant {errorRefPat.Identifier.Name}"
        else $"Update literal constant {errorRefPat.Identifier.Name} in signature"

    override x.IsAvailable _ =
        if isNull errorRefPat then false else

        match tryFindDeclarationFromSignature () with
        | Some sigDecl ->
            let sigMembers = sigDecl.As<IModuleDeclaration>().Members
            let sigBindingSignature = tryFindSigBindingSignature sigMembers
            match sigBindingSignature with
            | None -> false
            | Some s ->
                match s.HeadPattern with
                | :? IReferencePat as sRefPat ->
                    sigRefPat <- sRefPat
                    isImplExprValidInSig errorRefPat.Binding.Expression
                | _ -> false
        | _ -> false

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(sigRefPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let sigSymbolUse = sigRefPat.GetFcsSymbolUse()
        let implSymbolUse = errorRefPat.GetFcsSymbolUse()
        let implMfv = implSymbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let sigMfv = sigSymbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        if implMfv.FullType.BasicQualifiedName <> sigMfv.FullType.BasicQualifiedName then
            let returnTypeString = implMfv.ReturnParameter.Type.Format(sigSymbolUse.DisplayContext)
            let factory = sigRefPat.CreateElementFactory()
            let typeUsage = factory.CreateTypeUsage(returnTypeString, TypeUsageContext.TopLevel)
            sigRefPat.Binding.ReturnTypeInfo.SetReturnType(typeUsage) |> ignore

        sigRefPat.Binding.SetExpression(errorRefPat.Binding.Expression.Copy()) |> ignore

        // the FCS error can also mean that the impl side lacks the literal attribute        
        if implNeedsLiteralAttr then
            FSharpAttributesUtil.addAttributeListToLetBinding true (errorRefPat.Binding.As<IBinding>())
            let attrList = errorRefPat.Binding.AttributeLists[0]
            let attribute = errorRefPat.CreateElementFactory().CreateAttribute("Literal")
            FSharpAttributesUtil.addAttribute attrList attribute |> ignore
