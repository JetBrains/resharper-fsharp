namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type SyncLiteralConstantFix(error: ValueNotContainedMutabilityLiteralConstantValuesDifferError) =
    inherit FSharpQuickFixBase()

    let tryFindBindingSignature (declaredElement: IFSharpMember) =
        declaredElement.GetDeclarations()
        |> Seq.tryPick (function
            | :? IReferencePat as pat when pat.IsFSharpSigFile() -> Option.ofObj pat.Binding
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

    let mutable bindingSignature = null

    override x.Text = "Sync literal constant value to signature"

    override x.IsAvailable _ =
        match tryFindImplementationBindingInfo error.Pat with
        | None -> false
        | Some (topLevelBinding, declaredElement) ->
            match tryFindBindingSignature declaredElement with
            | None -> false
            | Some signature ->
                bindingSignature <- signature
                topLevelBinding.IsMutable <> signature.IsMutable

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Pat.IsPhysical())
        bindingSignature.SetIsMutable(not bindingSignature.IsMutable)
