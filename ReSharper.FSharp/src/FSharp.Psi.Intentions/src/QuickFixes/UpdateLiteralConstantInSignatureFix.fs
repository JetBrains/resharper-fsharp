namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type UpdateLiteralConstantInSignatureFix(error: LiteralConstantValuesDifferInSignatureError) =
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

    let mutable isUoM = false

    let rec isImplExprValidInSig (implExpression: IFSharpExpression) =
        
        let opName = $"{nameof UpdateLiteralConstantInSignatureFix}.IsAvailable"
        
        let rec collectUofMRefs (uOfM: IUnitOfMeasure) =
            seq {
                match uOfM with
                | :? INamedMeasure as named ->
                    yield named.TypeUsage.As<INamedTypeUsage>().ReferenceName.Reference
                | :? IProductMeasure as product ->
                    yield! collectUofMRefs product.Measure1
                    yield! collectUofMRefs product.Measure2
                | :? ISeqMeasure as seqM ->
                    for m in seqM.Measures do
                        yield! collectUofMRefs m
                | :? IDivideMeasure as divide ->
                    yield! collectUofMRefs divide.Measure1
                    yield! collectUofMRefs divide.Measure2
                | :? IPowerMeasure as power ->
                    yield! collectUofMRefs power.Measure
                | :? IParenMeasure as paren ->
                    yield! collectUofMRefs paren.Measure
                | _ -> ()
            }
    
        let isValidUofMInSig (uOfM: IUnitOfMeasureClause) =
            if isNull uOfM.Measure then false else
                
                let refs = collectUofMRefs uOfM.Measure
                refs |> Seq.forall (
                    fun r -> r.ResolveWithFcs(
                        sigRefPat, opName, false, true)
                            |> Option.isSome)

        
        match implExpression.IgnoreInnerParens() with
        | :? IReferenceExpr as refExpr ->
            refExpr.Reference.ResolveWithFcs(
                sigRefPat, opName, true, true)
            |> Option.isSome
        | :? IBinaryAppExpr as binExpr ->
            isImplExprValidInSig binExpr.LeftArgument && isImplExprValidInSig binExpr.RightArgument
        | :? ILiteralExpr as litExpr ->
            if isNull litExpr.UnitOfMeasure then true else
                isUoM <- true
                isValidUofMInSig litExpr.UnitOfMeasure
        | _ -> false

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
        // currenty FSharpType.Format is broken for UoM https://github.com/dotnet/fsharp/issues/15843
        if implMfv.FullType.BasicQualifiedName <> sigMfv.FullType.BasicQualifiedName && not isUoM then
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
