namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System.IO
open System.Text
open FSharp.Compiler.Symbols
open JetBrains.Application.UI.PopupLayout
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Psi.Naming
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

// TODO: what about attributes, type parameters, delegates, exceptions

// FSharpTokenType.AND.CreateLeafElement()

[<ContextAction(Group = "F#", Name = "Generate signature file for current file", Priority = 1s,
                Description = "Generate signature file for current file.")>]
type GenerateSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mkSignatureFile (fsharpFile: IFSharpFile): IFSharpFile  =
        let factory : IFSharpElementFactory = fsharpFile.CreateElementFactory(extension = FSharpSignatureProjectFileType.FsiExtension)
        let signatureFile : IFSharpFile = factory.CreateEmptyFile()
        let lineEnding = fsharpFile.GetLineEnding()
        let getName (decl: IFSharpDeclaration) = NamingManager.GetNamingLanguageService(fsharpFile.Language).MangleNameIfNecessary(decl.SourceName)

        let rec createModuleMemberSig (indentation: int) (moduleDecl: IModuleLikeDeclaration) (moduleMember: IModuleMember) : IFSharpTreeNode =
            match moduleMember with
            | :? ITypeDeclarationGroup as typeGroup ->
                // Filter out the IFSharpTypeDeclaration where we support the TypeRepresentation for now.
                let supportedTypeDeclarations =
                    typeGroup.TypeDeclarations
                    |> Seq.choose (function
                        | :? IFSharpTypeDeclaration as typeDecl ->
                            match typeDecl.TypeRepresentation with
                            | :? ITypeAbbreviationRepresentation
                            | :? ISimpleTypeRepresentation -> Some typeDecl
                            | _ -> None
                        | _ -> None)
                    |> Seq.mapi (fun idx typeDecl ->
                        let kw = if idx = 0 then "type" else "and"
                        {| SignatureIdx = idx ; TypeDeclaration = typeDecl; SourceText = $"{kw} {getName typeDecl} = int" |})
                    |> Seq.toArray

                if Array.isEmpty supportedTypeDeclarations then null else

                let sourceText = supportedTypeDeclarations |> Array.map (fun info -> info.SourceText) |> String.concat "\n"
                let sigTypeDeclarationGroup = factory.CreateModuleMember(sourceText) :?> ITypeDeclarationGroup

                if isNull sigTypeDeclarationGroup then null else

                for info in supportedTypeDeclarations do
                    let typeDecl: IFSharpTypeDeclaration = info.TypeDeclaration
                    let sigTypeDecl = sigTypeDeclarationGroup.TypeDeclarations.[info.SignatureIdx] :?> IFSharpTypeDeclaration
                    if isNull sigTypeDecl then () else

                    let sigMembers =
                        typeDecl.TypeMembers
                        |> Seq.choose (createMemberDeclaration >> Option.ofObj)

                    match typeDecl.TypeRepresentation with
                    | :? ITypeAbbreviationRepresentation as abbr ->
                        ModificationUtil.DeleteChildRange(sigTypeDecl.EqualsToken.NextSibling, sigTypeDecl.LastChild)
                        addNodesAfter sigTypeDecl.EqualsToken [
                            Whitespace()
                            abbr.Copy()
                            // TODO: there technically could be members here.
                            // Although I think this would need the `with` keyword.
                        ] |> ignore
                    | :? ISimpleTypeRepresentation as repr ->
                        ModificationUtil.DeleteChildRange(sigTypeDecl.EqualsToken.NextSibling, sigTypeDecl.LastChild)
                        addNodesAfter sigTypeDecl.EqualsToken [
                            NewLine(lineEnding)
                            Whitespace(indentation + moduleDecl.GetIndentSize())
                            repr.Copy()
                            for sigMember in sigMembers do
                                NewLine(lineEnding)
                                Whitespace(indentation + moduleDecl.GetIndentSize())
                                sigMember
                        ] |> ignore
                    | repr ->
                        // This pattern match should match the types we filtered out earlier for supportedTypeDeclarations
                        failwith $"Unexpected representation {repr.GetType()}"

                sigTypeDeclarationGroup

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
            | _ -> null

        and processModuleLikeDeclaration (indentation: int) (moduleDecl: IModuleLikeDeclaration) (moduleSig: IModuleLikeDeclaration) : IFSharpTreeNode =
            for moduleMember in moduleDecl.Members do
                let signatureMember = createModuleMemberSig indentation moduleDecl moduleMember

                if isNotNull signatureMember then
                    // newline + indentation whitespace
                    addNodesAfter moduleSig.LastChild [
                        NewLine(lineEnding)
                        Whitespace(indentation)
                        signatureMember
                    ]
                    |> ignore

            moduleSig

        and createMemberDeclaration (memberDecl: IFSharpTypeMemberDeclaration) : IFSharpTypeMemberDeclaration =
            match memberDecl with
            | :? IMemberDeclaration as memberDecl ->
                let sourceString =
                    let sb = StringBuilder()

                    if memberDecl.IsStatic then
                        sb.Append("static ") |> ignore

                    sb.Append(memberDecl.MemberKeyword.GetText()) |> ignore
                    sb.Append(" ") |> ignore

                    if isNotNull memberDecl.AccessModifier then
                        sb.Append(memberDecl.AccessModifier.GetText()) |> ignore

                    sb.Append(getName memberDecl) |> ignore
                    sb.Append(": ") |> ignore

                    let symbolUse = memberDecl.GetFcsSymbolUse()
                    if isNotNull symbolUse then
                        let mfv = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
                        if isNotNull mfv then
                            sb.Append(mfv.FullType.Format(symbolUse.DisplayContext)) |> ignore

                    sb.ToString()

                factory.CreateTypeMemberSignature(sourceString)
            | _ -> null

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
        // TODO: don't check has pair in unit test
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
