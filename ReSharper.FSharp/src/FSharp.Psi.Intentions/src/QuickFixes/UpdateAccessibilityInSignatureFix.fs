namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type UpdateAccessibilityInSignatureFix(error: ValueNotContainedMutabilityAccessibilityMoreError) =
    inherit FSharpQuickFixBase()

    let tryFindBindingSignatureAccessRights (declaredElement: IFSharpMember) =
        declaredElement.GetDeclarations()
        |> Seq.tryPick (function
            | :? IReferencePat as pat when pat.IsFSharpSigFile() ->
                match pat.DeclaredElement.As<IFSharpMember>() with
                | null -> None
                | sigMember ->
                    let bindingSignature = BindingSignatureNavigator.GetByHeadPattern(pat)
                    if isNull bindingSignature then None else
                    Some (bindingSignature, sigMember.GetAccessRights())
            | _ -> None
        )

    let tryFindImplementationBindingInfo (pat: ITopReferencePat) =
        if isNull pat then None else

        match pat.DeclaredElement.As<IFSharpMember>() with
        | null -> None
        | fsMember -> Some fsMember

    let mutable implAccessRights = AccessRights.NONE
    let mutable bindingSignature = null

    override x.Text = $"Update accessibility for {error.ReferenceName.Identifier.Name} in signature"

    override x.IsAvailable _ =
        let topPat = TopReferencePatNavigator.GetByReferenceName(error.ReferenceName)
        match tryFindImplementationBindingInfo topPat with
        | None -> false
        | Some implDeclaredElement ->
            match tryFindBindingSignatureAccessRights implDeclaredElement with
            | None -> false
            | Some (bindingSig, sigAccessRights) ->
                implAccessRights <- implDeclaredElement.GetAccessRights()
                bindingSignature <- bindingSig
                implAccessRights <> sigAccessRights

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.ReferenceName.IsPhysical())
        bindingSignature.SetAccessModifier(implAccessRights)
