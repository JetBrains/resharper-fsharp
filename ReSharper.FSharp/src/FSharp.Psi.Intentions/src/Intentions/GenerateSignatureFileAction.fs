namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System.IO
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open type JetBrains.ReSharper.Psi.PsiSourceFileExtensions
open JetBrains.ReSharper.Plugins.FSharp
open  JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

type TopLevelModuleOrNamespace =
    {
        IsModule: bool
        Name: string
        NestedModules: NestedModule list
    }

and NestedModule =
    {
        Name: string
        NestedModules: NestedModule list
    }

[<ContextAction(Group = "F#", Name = "Generate signature file for current file", Priority = 1s,
                Description = "Generate signature file for current file.")>]
type GenerateSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    
    let mkSignatureFile (fsharpFile: IFSharpFile) : IFSharpFile =
        let factory : IFSharpElementFactory = fsharpFile.CreateElementFactory()
        let signatureFile : IFSharpFile = factory.CreateEmptyFile()
        
        let rec processModuleMembers (parent: IFSharpTreeNode) (members: IModuleMember seq) =
            for m in members do
                match m with
                | :? INestedModuleDeclaration as nmd ->
                    let nestedModuleNode = factory.CreateNestedModule(nmd.NameIdentifier.Name)
                    // TODO: if the nested module has nested modules, clear the content (`begin end`) and process them.
                    ModificationUtil.AddChild(parent, nestedModuleNode) |> ignore
                | _ -> ()
        
        for decl in fsharpFile.ModuleDeclarations do
            match decl with
            | :? INamedModuleDeclaration as nmd ->
                let moduleNode = factory.CreateModule(nmd.NameIdentifier.Name)
                processModuleMembers moduleNode nmd.Members
                ModificationUtil.AddChild(signatureFile, moduleNode) |> ignore
            | :? INamedNamespaceDeclaration as nnd ->
                let namespaceNode = factory.CreateModule(nnd.NameIdentifier.Name)
                processModuleMembers namespaceNode nnd.Members
                ModificationUtil.AddChild(signatureFile, namespaceNode) |> ignore
            | _ -> ()

        signatureFile

    override this.Text = "Generate signature file for current file"
    
    override this.IsAvailable _ =
        let solution = dataProvider.Solution
        let isSettingEnabled = solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.GenerateSignatureFile)
        if not isSettingEnabled then false else
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        not hasSignature
        
    override this.ExecutePsiTransaction(solution, _) =
        let projectFile = dataProvider.SourceFile.ToProjectFile()
        let fsharpFile = projectFile.GetPrimaryPsiFile().AsFSharpFile()
        let physicalPath = dataProvider.SourceFile.ToProjectFile().Location.FileAccessPath
        let fsiFile = Path.ChangeExtension(physicalPath, ".fsi")
        
        let signatureFile = mkSignatureFile fsharpFile
        // try
        //     let currentFSharpFile = dataProvider.PsiFile
        //     let fcsService = currentFSharpFile.FcsCheckerService
        //     let checkResult = fcsService.ParseAndCheckFile(currentFSharpFile.GetSourceFile(), "for signature file", true)
        //     do
        //         match checkResult with
        //         | None -> ()
        //         | Some { CheckResults = checkResult } ->
        //         
        //         match checkResult.GenerateSignature() with
        //         | None -> ()
        //         | Some signatureSourceText ->
        //             let content = string signatureSourceText
        //             File.WriteAllText(fsiFile, content)
        // with ex ->
        //     // TODO: show some balloon thing?
        //     ()

        // solution.InvokeUnderTransaction(fun transactionCookie ->
        //     let virtualPath = FileSystemPath.TryParse(fsiFile).ToVirtualFileSystemPath()
        //     let relativeTo = RelativeTo(projectFile, RelativeToType.Before)
        //     transactionCookie.AddFile(projectFile.ParentFolder, virtualPath, context = OrderingContext(relativeTo))
        //     |> ignore)

        null