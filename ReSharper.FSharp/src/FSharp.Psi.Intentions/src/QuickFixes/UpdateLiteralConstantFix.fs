namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type UpdateLiteralConstantFix(error: LiteralConstantValuesDifferError) =
    inherit FSharpQuickFixBase()
    
    let tryFindSigFile (topRefPat: ITopReferencePat) =
        let containing = topRefPat.GetContainingTypeDeclaration()
        let decls = containing.DeclaredElement.GetDeclarations()
        decls |> Seq.tryFind (fun d -> d.GetSourceFile().IsFSharpSignatureFile)
        
    let tryFindSigBindingSignature sigMembers =
        sigMembers
        |>  Seq.tryPick(fun m ->
                let bindingSignature = m.As<IBindingSignature>()
                match bindingSignature with
                | null -> None
                | _ ->
                    match error.Pat.Binding.HeadPattern with
                    | :? IReferencePat as implPat ->
                        match bindingSignature.HeadPattern with
                        | :? IReferencePat as sigRefPat when
                            implPat.DeclaredName = sigRefPat.DeclaredName
                            -> Some bindingSignature
                        | _ -> None
                    | _ -> None
                )

    let mutable sigRefPat = null

    override x.Text = $"Update literal constant {error.Pat.Identifier.Name} in signature"

    override x.IsAvailable _ =
        // Todo reuse/extend SignatureFixUtil
        match tryFindSigFile error.Pat with
        | None -> false
        | Some sigFile ->
            let sigMembers = sigFile.As<IModuleDeclaration>().Members
            let sigBindingSignature = tryFindSigBindingSignature sigMembers
            match sigBindingSignature with
            | None -> false
            | Some s ->
                match s.HeadPattern with
                | :? IReferencePat as sRefPat ->
                    sigRefPat <- sRefPat
                    true
                | _ -> false

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(sigRefPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let sigSymbolUse = sigRefPat.GetFcsSymbolUse()
        let implSymbolUse = error.Pat.As<IReferencePat>().GetFcsSymbolUse()
        let implMfv = implSymbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let sigMfv = sigSymbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        if implMfv.FullType.BasicQualifiedName <> sigMfv.FullType.BasicQualifiedName then
            let returnTypeString = implMfv.ReturnParameter.Type.Format(sigSymbolUse.DisplayContext)
            let factory = sigRefPat.CreateElementFactory()
            let typeUsage = factory.CreateTypeUsage(returnTypeString, TypeUsageContext.TopLevel)
            sigRefPat.Binding.ReturnTypeInfo.SetReturnType(typeUsage) |> ignore

        sigRefPat.Binding.SetExpression(error.Pat.Binding.Expression.Copy()) |> ignore
