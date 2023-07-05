namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type UpdateMutabilityInSignatureFix(error: ValueNotContainedMutabilityAttributesDifferError) =
    inherit FSharpQuickFixBase()

    let tryFindBindingSignature (implementationDeclaredName: string) (declaredElement: IFSharpMember) =
        let parentDeclarations = declaredElement.ContainingType.GetDeclarations()

        // Find the parent signature counter part
        let signatureNameModuleDeclaration =
            parentDeclarations
            |> Seq.tryPick (fun d ->
                match d with
                | :? INamedModuleDeclaration as signatureModule when
                    signatureModule.GetSourceFile().IsFSharpSignatureFile -> Some signatureModule
                | _ -> None)

        signatureNameModuleDeclaration
        |> Option.bind (fun signatureModule ->
            signatureModule.Members
            |> Seq.tryPick (fun sigMember ->
                match sigMember with
                | :? IBindingSignature as bindingSignature ->
                    match bindingSignature.HeadPattern with
                    | :? ITopReferencePat as bindingNamePat
                        // This is a bit lucky to work.
                        // There must be something more clever we can try
                        when bindingNamePat.DeclaredName = implementationDeclaredName ->
                        Some bindingSignature
                    | _ -> None
                | _ -> None)
        )

    let tryFindImplementationBindingInfo (pat: ITopReferencePat) =
        if isNull pat then None else

        match pat.Binding, pat.DeclaredElement.As<IFSharpMember>() with
        | null, _ | _, null -> None
        | binding, fsMember ->

        let mfv = fsMember.Mfv
        if isNull mfv then None else

        Some(binding, fsMember)

    override x.Text = "Update mutability in signature"

    override x.IsAvailable _ =
        match tryFindImplementationBindingInfo error.Pat with
        | None -> false
        | Some (topLevelBinding, declaredElement) ->
            match tryFindBindingSignature error.Pat.DeclaredName declaredElement with
            | None -> false
            | Some bindingSignature -> topLevelBinding.IsMutable <> bindingSignature.IsMutable

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match tryFindImplementationBindingInfo error.Pat with
        | None -> ()
        | Some (topLevelBinding, declaredElement) ->
            match tryFindBindingSignature error.Pat.DeclaredName declaredElement with
            | None -> ()
            | Some bindingSignature ->
                bindingSignature.SetIsMutable(topLevelBinding.IsMutable)
