namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type UpdateLiteralConstantFix(error: ValueNotContainedMutabilityLiteralConstantValuesDifferError) =
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

    let mutable sigBinding = null
    let mutable implExpr = null

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
                | :? IReferencePat as sigRefPat ->
                    sigBinding <- sigRefPat.Binding
                    implExpr <- error.Pat.Binding.Expression
                    true
                | _ -> false

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(sigBinding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        // Todo update type in sig if needed
        sigBinding.SetExpression(implExpr.Copy()) |> ignore
