namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

type UpdateSignatureFileFix(binding: IBinding) =
    inherit FSharpQuickFixBase()

    let moduleDecl =
        if isNull binding then None else
        let letBinding = LetBindingsDeclarationNavigator.GetByBinding(binding)
        if isNull letBinding then None else
        let moduleDecl = ModuleDeclarationNavigator.GetByMember(letBinding)
        if isNull moduleDecl then None else Some (letBinding, moduleDecl)
    
    new (error: SignatureFileMismatchError) =
        UpdateSignatureFileFix(BindingNavigator.GetByHeadPattern(error.Pattern))

    new (warning: ArgumentNameMismatchWarning) =
        // The name is wrong in this case
        // The parameter pattern is two levels higher
        match warning.Pattern.Parent with
        | :? ITypedPat as tp ->
            match tp.Parent with
            | :? IParenPat as pat ->
                UpdateSignatureFileFix(BindingNavigator.GetByParameterPattern(pat))
            | _ -> UpdateSignatureFileFix(Unchecked.defaultof<IBinding>)
        | _ -> UpdateSignatureFileFix(Unchecked.defaultof<IBinding>)
    
    override this.Text = "Update signature file"
    
    override this.IsAvailable _ =
        Option.isSome moduleDecl
    
    override this.ExecutePsiTransaction _ =        
        match moduleDecl with
        | None -> ()
        | Some (letBindings, moduleDecl) ->

        match SignatureFile.tryMkBindingSignature letBindings moduleDecl with
        | None -> ()
        | Some (sigDeclNode, _, sigFile) ->
            
        let implHeadPat =
            let binding = letBindings.Bindings.FirstOrDefault() in
            if isNull binding then null else
            match binding.HeadPattern with
            | :? ITopReferencePat as pat -> pat
            | _ -> null

        if isNull implHeadPat then () else

        let mdl = sigFile.ModuleDeclarations.FirstOrDefault()
        let existingBinding =
            Seq.cast<IBindingSignature> mdl.MembersEnumerable
            |> Seq.tryFind (fun b ->
                match b.HeadPattern with
                | :? ITopReferencePat as sigPat ->
                    sigPat.CompiledName = implHeadPat.CompiledName
                | _ -> false)

        match existingBinding with
        | None -> ()
        | Some existingBinding ->

        use writeCookie = WriteLockCookie.Create(implHeadPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        ModificationUtil.ReplaceChild(existingBinding, sigDeclNode) |> ignore
