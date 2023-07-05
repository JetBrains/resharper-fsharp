namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type UpdateMutabilityInSignatureFix(error: ValueNotContainedMutabilityAttributesDifferError) =
    inherit FSharpQuickFixBase()

    let tryFindBindingSignature (declaredElement: IFSharpMember) =
        declaredElement.GetDeclarations()
        |> Seq.tryPick (fun decl ->
            if not (decl.GetSourceFile().IsFSharpSignatureFile) then
                None
            else
                match decl with
                | :? ITopReferencePat as bindingSignaturePat ->
                    match bindingSignaturePat.Parent with
                    | :? IBindingSignature as bindingSignature -> Some bindingSignature
                    | _ -> None
                | _ -> None
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
            match tryFindBindingSignature declaredElement with
            | None -> false
            | Some bindingSignature -> topLevelBinding.IsMutable <> bindingSignature.IsMutable

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match tryFindImplementationBindingInfo error.Pat with
        | None -> ()
        | Some (topLevelBinding, declaredElement) ->
            match tryFindBindingSignature declaredElement with
            | None -> ()
            | Some bindingSignature ->
                bindingSignature.SetIsMutable(topLevelBinding.IsMutable)
