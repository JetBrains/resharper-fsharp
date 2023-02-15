namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System.IO
open JetBrains.Application.UI.PopupLayout
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Psi.Tree
open JetBrains.DocumentManagers.Transactions.ProjectHostActions.Ordering
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.RdBackend.Common.Features.ProjectModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open  JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

// extract value -> ctrl alt v
// undo that -> ctrl alt n

[<ContextAction(Group = "F#", Name = "Generate signature file for current file", Priority = 1s,
                Description = "Generate signature file for current file.")>]
type GenerateSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mkSignatureFile (fsharpFile: IFSharpFile) : IFSharpFile =
        let factory : IFSharpElementFactory = fsharpFile.CreateElementFactory()
        let signatureFile : IFSharpFile = factory.CreateEmptyFile()
        let lineEnding = fsharpFile.GetLineEnding()

        let rec processModuleLikeDeclaration (indentation: int) (moduleDecl: IModuleLikeDeclaration) (moduleSig: IModuleLikeDeclaration) : IFSharpTreeNode =
            for moduleMember in moduleDecl.Members do
                // newline + indentation whitespace
                addNodesAfter moduleSig.LastChild [
                    NewLine(lineEnding)
                    Whitespace(indentation)
                    match moduleMember with
                    | :? INestedModuleDeclaration as nestedNestedModule ->
                        let nestedSigModule = factory.CreateNestedModule(nestedNestedModule.NameIdentifier.Name)
                        let members = nestedNestedModule.Members
                        let shouldEmptyContent =
                            not members.IsEmpty
                            && members |> Seq.forall (function | :? IExpressionStatement -> false | _ -> true)

                        if shouldEmptyContent then
                            ModificationUtil.DeleteChildRange (nestedSigModule.EqualsToken.NextSibling, nestedSigModule.LastChild)
                        processModuleLikeDeclaration (indentation + moduleDecl.GetIndentSize()) nestedNestedModule nestedSigModule
                    | :? IOpenStatement as openStatement ->
                        openStatement.Copy()
                    | _ -> ()
                ]
                |> ignore

            moduleSig
        
        for decl in fsharpFile.ModuleDeclarations do
            let signatureModule : IModuleLikeDeclaration =
                match decl with
                | :? INamedModuleDeclaration as nmd ->
                    factory.CreateModule(nmd.DeclaredElement.GetClrName().FullName)
                | :? IGlobalNamespaceDeclaration ->
                    factory.CreateNamespace("global") :?> _
                | :? INamedNamespaceDeclaration as nnd ->
                    // TODO: add an interface that could unify named and global namespace.
                    factory.CreateNamespace(nnd.QualifiedName) :?> _
                | decl -> failwithf $"Unexpected declaration, got: %A{decl}"

            ModificationUtil.AddChildAfter(signatureModule.LastChild, NewLine(lineEnding)) |> ignore
            let signatureModule = processModuleLikeDeclaration 0 decl signatureModule 
            ModificationUtil.AddChild(signatureFile, signatureModule) |> ignore

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
        File.WriteAllText(fsiFile, signatureFile.GetText())

        solution.InvokeUnderTransaction(fun transactionCookie ->
            let virtualPath = FileSystemPath.TryParse(fsiFile).ToVirtualFileSystemPath()
            let relativeTo = RelativeTo(projectFile, RelativeToType.Before)
            let projectFile = transactionCookie.AddFile(projectFile.ParentFolder, virtualPath, context = OrderingContext(relativeTo))
            
            if (not Shell.Instance.IsTestShell) then
                let navigationOptions = NavigationOptions.FromWindowContext(Shell.Instance.GetComponent<IMainWindowPopupWindowContext>().Source, "")
                NavigationManager
                    .GetInstance(solution)
                    .Navigate(
                        ProjectFileNavigationPoint(projectFile),
                        navigationOptions
                    )
                    |> ignore
        )

        null
        
        // First test name would be: ``ModuleStructure 01`` , ``NamespaceStructure 01``
        
        // TODO: raise parser issue.