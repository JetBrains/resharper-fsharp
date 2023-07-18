namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type UpdateAccessibilityInSignatureMemberFix(error: ValueNotContainedMutabilityAccessibilityMoreInMemberError) =
    inherit FSharpQuickFixBase()

    let tryFindSignatureMemberAccessRights (memberDeclaration: IOverridableMemberDeclaration) =
        if isNull memberDeclaration.DeclaredElement then None else
        memberDeclaration.DeclaredElement.GetDeclarations()
        |> Seq.tryPick (function
            | :? IMemberSignature as memberSig -> Some (memberSig, memberSig.GetAccessRights())
            | _ -> None)
    let mutable implAccessRights = AccessRights.NONE
    let mutable memberSignature = null

    override x.Text = $"Update accessibility for {error.MemberDeclaration.Identifier.GetText()} in signature"

    override x.IsAvailable _ =
        if isNull error.MemberDeclaration then false else
        implAccessRights <- error.MemberDeclaration.GetAccessRights()

        match tryFindSignatureMemberAccessRights error.MemberDeclaration with
        | None -> false
        | Some (ms, sigAccessRights) ->
            memberSignature <- ms
            implAccessRights <> sigAccessRights

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.MemberDeclaration.IsPhysical())
        memberSignature.SetAccessModifier(implAccessRights)
