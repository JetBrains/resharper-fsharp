namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell

type UpdateAccessibilityInSignatureMemberFix(error: ValueNotContainedMutabilityAccessibilityMoreInMemberError) =
    inherit FSharpQuickFixBase()

    let tryFindSignatureMemberAccessRights (memberDeclaration: IOverridableMemberDeclaration) =
        if isNull memberDeclaration.DeclaredElement then None else
        memberDeclaration.DeclaredElement.GetDeclarations()
        |> Seq.tryPick (function
            | :? IMemberSignature as memberSig -> Some (memberSig, memberSig.GetAccessRights())
            | _ -> None)
        
    let tryFindSignatureMemberInPropertyAccessRights (memberDeclaration: IMemberDeclaration) (accessorDeclaration:IAccessorDeclaration)  =
        if isNull memberDeclaration.DeclaredElement then None else
        let implAccessRights = accessorDeclaration.GetAccessRights()
        memberDeclaration.DeclaredElement.GetDeclarations()
        |> Seq.tryPick (function
            | :? IMemberSignature as memberSig ->
                memberSig.AccessorDeclarationsEnumerable
                |> Seq.tryFind (fun ad -> ad.DeclaredName = accessorDeclaration.DeclaredName) 
                |> Option.bind (fun _ ->
                    if implAccessRights <> memberSig.GetAccessRights() then
                        Some (memberSig, accessorDeclaration.DeclaredName, implAccessRights)
                    else None)
            | _ -> None)

    let mutable implAccessRights = AccessRights.NONE
    let mutable memberSignature = null

    override x.Text = $"Update accessibility for {error.MemberDeclaration.Identifier.GetText()} in signature"

    override x.IsAvailable _ =
        if isNull error.MemberDeclaration then false else

        match error.MemberDeclaration with
        | :? IMemberDeclaration as md when md.AccessorDeclarations.Count = 2 ->
            // property with get/set
            let properties =
                md.AccessorDeclarationsEnumerable
                |> Seq.choose (fun ad -> tryFindSignatureMemberInPropertyAccessRights md ad)
                |> Seq.toList

            // TODO: check for scenario when more than one property has different access rights
            
            match properties with
            | [ memberSig, _, implAccR ] ->
                implAccessRights <- implAccR
                memberSignature <- memberSig
                true
            | _ -> false
        | _ ->

        implAccessRights <- error.MemberDeclaration.GetAccessRights()

        match tryFindSignatureMemberAccessRights error.MemberDeclaration with
        | None -> false
        | Some (ms, sigAccessRights) ->
            memberSignature <- ms
            implAccessRights <> sigAccessRights

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.MemberDeclaration.IsPhysical())
        memberSignature.SetAccessModifier(implAccessRights)
