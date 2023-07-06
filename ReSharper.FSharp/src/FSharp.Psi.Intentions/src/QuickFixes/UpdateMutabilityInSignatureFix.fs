namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.SignatureFixUtil

type UpdateMutabilityInSignatureFix(error: ValueNotContainedMutabilityAttributesDifferError) =
    inherit FSharpQuickFixBase()

    let mutable bindingSignature = null

    override x.Text = $"Update mutability for {error.Pat.Identifier.Name} in signature"

    override x.IsAvailable _ =
        match tryFindBindingPairFromTopReferencePat error.Pat with
        | None -> false
        | Some (BindingPair(implBinding = topLevelBinding; sigBinding = signature)) ->
            bindingSignature <- signature
            topLevelBinding.IsMutable <> signature.IsMutable

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Pat.IsPhysical())
        bindingSignature.SetIsMutable(not bindingSignature.IsMutable)
