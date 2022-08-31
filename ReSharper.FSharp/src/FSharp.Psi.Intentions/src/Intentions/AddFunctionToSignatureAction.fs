namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

[<ContextAction(Name = "AddFunctionToSignatureFile", Group = "F#", Description = "Add function to signature file")>]
type AddFunctionToSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()
    
    let getSignatureLocation () =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then None else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then None else
        
        match bindings.First().HeadPattern.As<IReferencePat>() with
        | null -> None
        | pat -> pat.GetFcsSymbol().SignatureLocation

    let addChildAfter (newlineNode: ITreeNode) (insertNewline: bool) (child: ITreeNode) (anchor: ITreeNode) =
        let anchor =
            let anchor =
                if anchor.NodeType <> newlineNode.NodeType then
                    ModificationUtil.AddChildAfter(anchor, newlineNode)
                else
                    anchor

            if insertNewline then
                ModificationUtil.AddChildAfter(anchor, newlineNode)
            else
                anchor

        ModificationUtil.AddChildAfter(ModificationUtil.AddChildAfter(anchor, child), newlineNode)
    
    override x.Text = "Add to signature file"
    override this.IsAvailable _ =
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        if not hasSignature then false else
        
        match getSignatureLocation () with
        | None -> false
        | Some range -> not (range.FileName.EndsWith(".fsi"))

    override this.ExecutePsiTransaction(solution, progress) =
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()
        let letBindings = dataProvider.GetSelectedElement<ILetBindingsDeclaration>()
        let moduleDecl = ModuleDeclarationNavigator.GetByMember(letBindings)

        match SignatureFile.tryMkBindingSignature letBindings moduleDecl with
        | None -> null
        | Some response ->
            let sigFile = response.SigFile
            let openStatements = response.OpenStatements
            let newlineNode = NewLine(response.SigDeclNode.GetLineEnding()) :> ITreeNode
            let addChildAfter : bool -> ITreeNode -> ITreeNode -> ITreeNode  = addChildAfter newlineNode

            let lastExistingOpenStatement =
                response.SigModule.Members
                |> Seq.choose (function | :? IOpenStatement as os -> Some os | _ -> None)
                |> Seq.tryLast
            
            let addOpeningStatements node =
                (node, openStatements)
                ||> List.fold (fun acc openStatement -> addChildAfter false openStatement acc)
            
            let lastNode =
                match lastExistingOpenStatement, openStatements.IsEmpty with
                | Some lastOpenStatement, false ->
                    // TODO: this won't work if there are other vals in the signature file
                    addOpeningStatements lastOpenStatement
                | None, false ->
                    addOpeningStatements (ModificationUtil.AddChildAfter(sigFile.LastChild, newlineNode))
                | _, true ->
                    sigFile.LastChild

            lastNode
            |> addChildAfter true response.SigDeclNode
            |> ignore

            null