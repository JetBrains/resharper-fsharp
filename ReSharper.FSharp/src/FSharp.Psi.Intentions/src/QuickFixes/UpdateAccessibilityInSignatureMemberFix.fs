namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
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

        let allDeclarations : IDeclaration array = memberDeclaration.DeclaredElement.GetDeclarations() |> Seq.toArray
        let allDeclarations =
            if allDeclarations.Length > 1 then allDeclarations  else

            // Search via the parent
            let fm = memberDeclaration.DeclaredElement.As<IFSharpMember>()
            if isNull fm then Array.empty else

            fm.ContainingType.GetDeclarations()
            |> Seq.tryPick (fun t ->
                match t with
                | :? IFSharpTypeDeclaration as td when td.IsFSharpSigFile() ->
                    td.TypeMembers
                    |> Seq.choose (function | :? IDeclaration as decl -> Some decl | _ -> None)
                    |> Seq.toArray
                    |> Some
                | _ -> None)
            |> Option.defaultValue Array.empty

        allDeclarations
        |> Seq.tryPick (function
            | :? IMemberSignature as memberSig ->
                memberSig.AccessorDeclarationsEnumerable
                |> Seq.tryFind (fun ad -> ad.DeclaredName = accessorDeclaration.DeclaredName)
                |> Option.bind (fun _ ->
                    if implAccessRights <> memberSig.GetAccessRights() then
                        Some (memberSig, accessorDeclaration.DeclaredName, implAccessRights)
                    else None)
            | _ -> None)

    let updatableSignatures : ResizeArray<IMemberSignature * AccessRights> = ResizeArray()

    override x.Text = $"Update accessibility for {error.MemberDeclaration.Identifier.GetText()} in signature"

    override x.IsAvailable _ =
        if isNull error.MemberDeclaration then false else
        // If both the get and set of a property are reported, IsAvailable will be called twice.
        if updatableSignatures.Count > 0 then true else

        match error.MemberDeclaration with
        | :? IMemberDeclaration as md when md.AccessorDeclarations.Count = 2 ->
            // property with get/set
            md.AccessorDeclarationsEnumerable
            |> Seq.choose (tryFindSignatureMemberInPropertyAccessRights md)
            |> Seq.iter (fun (memberSig, _declName, implAccR) -> updatableSignatures.Add (memberSig, implAccR))
        | _ ->
            match tryFindSignatureMemberAccessRights error.MemberDeclaration with
            | None -> ()
            | Some (ms, sigAccessRights) ->
                let implAccessRights = error.MemberDeclaration.GetAccessRights()
                if implAccessRights <> sigAccessRights then
                    updatableSignatures.Add (ms, implAccessRights)

        updatableSignatures.Count > 0

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.MemberDeclaration.IsPhysical())
        for memberSignature, implAccessRights in updatableSignatures do
            memberSignature.SetAccessModifier(implAccessRights)
