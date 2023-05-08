namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System.IO
open System.Text
open FSharp.Compiler.Symbols
open JetBrains.Application.Threading
open JetBrains.Application.UI.PopupLayout
open JetBrains.DocumentManagers.Transactions.ProjectHostActions.Ordering
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.RdBackend.Common.Features.ProjectModel
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Feature.Services.Generate.Actions
open JetBrains.ReSharper.Feature.Services.Generate.Workflows
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

module FSharpGeneratorKinds =
    let [<Literal>] SignatureFile = "SignatureFile"

type FSharpGeneratorSignatureElement(fsFile: IFSharpFile) =
    inherit GeneratorElementBase()

    override this.GetPresentationObject() = fsFile
    override this.Matches(_searchText, matcher) = matcher.Matches(this.TestDescriptor)
    override this.TestDescriptor = "Generate signature file title" // fsFile.GetSourceFile().Name
    
    interface IGeneratorElementPresenter with
        member this.InitGeneratorPresenter(presenter) =
            presenter.Present<FSharpGeneratorSignatureElement>(fun value item structureelement state ->
                item.RichText <-
                    // Text seen in the popup of the selectable item.
                    JetBrains.UI.RichText.RichText(fsFile.GetSourceFile().Name)
                item.Images.Add(PsiServicesThemedIcons.HasImplementations.Id))

[<GeneratorElementProvider(FSharpGeneratorKinds.SignatureFile, typeof<FSharpLanguage>)>]
type FSharpGenerateSignatureProvider() =
    inherit GeneratorProviderBase<FSharpGeneratorContext>()

    override this.Populate(context: FSharpGeneratorContext): unit =
        let node = context.Root :?> IFSharpTreeNode
        context.ProvidedElements.Add(FSharpGeneratorSignatureElement(node.FSharpFile))

[<GeneratorBuilder(FSharpGeneratorKinds.SignatureFile, typeof<FSharpLanguage>)>]
type FSharpGenerateSignatureBuilder() =
    inherit GeneratorBuilderBase<FSharpGeneratorContext>()
    
    // TODO: what about attributes, type parameters
    
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
                            | :? IStructRepresentation
                            | :? ISimpleTypeRepresentation
                            | :? IDelegateRepresentation
                            // Regular classes have no representation.
                            | null -> Some typeDecl
                            | _ -> None
                        | _ -> None)
                    |> Seq.mapi (fun idx typeDecl ->
                        let kw = if idx = 0 then "type" else "and"
                        {| SignatureIdx = idx ; TypeDeclaration = typeDecl; SourceText = $"{kw} {getName typeDecl} = int" |})
                    |> Seq.toArray

                if Array.isEmpty supportedTypeDeclarations then null else

                let sourceText = supportedTypeDeclarations |> Array.map (fun info -> info.SourceText) |> String.concat lineEnding
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
                    | :? IStructRepresentation as repr ->
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
                    | :? IDelegateRepresentation as repr ->
                        ModificationUtil.DeleteChildRange(sigTypeDecl.EqualsToken.NextSibling, sigTypeDecl.LastChild)
                        addNodesAfter sigTypeDecl.EqualsToken [
                            Whitespace()
                            repr.Copy()
                        ] |> ignore
                    | null ->
                        ModificationUtil.DeleteChildRange(sigTypeDecl.EqualsToken.NextSibling, sigTypeDecl.LastChild)
                        addNodesAfter sigTypeDecl.EqualsToken [
                            NewLine(lineEnding)
                            Whitespace(indentation + moduleDecl.GetIndentSize())
                            if isNotNull typeDecl.PrimaryConstructorDeclaration then
                                yield! createPrimaryConstructorSignature (getName typeDecl) typeDecl.PrimaryConstructorDeclaration
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
            | :? ILetBindingsDeclaration as letBindingsDeclaration ->
                
                let sourceString (binding: IBinding) =
                    let sb = StringBuilder()

                    sb.Append("val ") |> ignore
                    sb.Append(binding.HeadPattern.GetText()) |> ignore
                    sb.Append(": ") |> ignore
                    
                    let refPat = binding.HeadPattern.As<IReferencePat>()
                    if isNotNull refPat then
                        let symbolUse = refPat.GetFcsSymbolUse()
                        if isNotNull symbolUse then
                            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
                            sb.Append(mfv.FullType.Format(symbolUse.DisplayContext)) |> ignore
                  
                    sb.ToString()

                let sigStrings =
                    Seq.map sourceString letBindingsDeclaration.Bindings
                    |> String.concat lineEnding
                
                factory.CreateTypeMember(sigStrings)
            | :? IExceptionDeclaration as exceptionDeclaration ->
                let sigExceptionDeclaration = exceptionDeclaration.Copy()

                if not exceptionDeclaration.TypeMembers.IsEmpty then
                    let sigMembers =
                        exceptionDeclaration.TypeMembers
                        |> Seq.choose (createMemberDeclaration >> Option.ofObj)

                    ModificationUtil.DeleteChildRange(sigExceptionDeclaration.WithKeyword.NextSibling, sigExceptionDeclaration.LastChild)

                    addNodesAfter sigExceptionDeclaration.WithKeyword [
                        for sigMember in sigMembers do
                            NewLine(lineEnding)
                            Whitespace(indentation + moduleDecl.GetIndentSize())
                            sigMember
                    ] |> ignore

                sigExceptionDeclaration
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
                            if memberDecl.IsStatic then
                                sb.Append(mfv.FullType.Format(symbolUse.DisplayContext)) |> ignore
                            else
                                // mfv.FullType will contain the type of the instance, so we cannot use that.
                                let parameters =
                                    mfv.CurriedParameterGroups
                                    |> Seq.map (fun parameterGroup ->
                                        parameterGroup
                                        |> Seq.map (fun parameter -> parameter.Type.Format(symbolUse.DisplayContext))
                                        |> String.concat " * "
                                    )
                                    |> String.concat " -> "
                                let returnType = mfv.ReturnParameter.Type.Format(symbolUse.DisplayContext)
                                sb.Append($"{parameters} -> {returnType}") |> ignore

                    sb.ToString()

                factory.CreateTypeMember(sourceString)
            | _ -> null

        and createPrimaryConstructorSignature (typeName: string) (primaryConstructorDeclaration: IPrimaryConstructorDeclaration) : ITreeNode seq =
            let symbolUse = primaryConstructorDeclaration.GetFcsSymbolUse()
            if isNull symbolUse then Seq.empty else
            let mfv = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
            if isNull mfv then Seq.empty else
            let parameters =
                mfv.CurriedParameterGroups
                |> Seq.map (fun parameterGroup ->
                    parameterGroup
                    |> Seq.map (fun parameter -> parameter.Type.Format(symbolUse.DisplayContext))
                    |> String.concat " * "
                )
                |> String.concat " -> "
            factory.CreateTypeMember $"new: {parameters} -> {typeName}"
            :> ITreeNode
            |> Seq.singleton

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

    override this.IsAvailable(context: FSharpGeneratorContext): bool =
        let node = context.Root :?> IFSharpTreeNode
        let currentFSharpFile = node.FSharpFile
        if currentFSharpFile.IsFSharpSigFile() then false else

        let solution = node.GetSolution()
        let isSettingEnabled = solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.GenerateSignatureFile)
        if not isSettingEnabled then false else

        let fcsService = currentFSharpFile.FcsCheckerService
        // TODO: don't check has pair in unit test
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile (node.GetSourceFile())
        not hasSignature

    override this.Process(context) =
        let node = context.Root :?> IFSharpTreeNode
        use writeCookie = WriteLockCookie.Create(node.IsPhysical())

        let projectFile = node.GetSourceFile().ToProjectFile()
        let physicalPath = projectFile.Location.FileAccessPath
        let fsiFile = Path.ChangeExtension(physicalPath, ".fsi")
        let signatureFile = mkSignatureFile node.FSharpFile
        File.WriteAllText(fsiFile, signatureFile.GetText())

        let solution = context.Solution
        solution.Locks.ExecuteOrQueue(FSharpGeneratorKinds.SignatureFile, fun _ ->
            solution.InvokeUnderTransaction(fun transactionCookie ->
                let virtualPath = FileSystemPath.TryParse(fsiFile).ToVirtualFileSystemPath()
                let relativeTo = RelativeTo(projectFile, RelativeToType.Before)
                let projectFile = transactionCookie.AddFile(projectFile.ParentFolder, virtualPath, context = OrderingContext(relativeTo))

                if Shell.Instance.IsTestShell then () else

                let navigationOptions = NavigationOptions.FromWindowContext(Shell.Instance.GetComponent<IMainWindowPopupWindowContext>().Source, "")
                NavigationManager
                    .GetInstance(solution)
                    .Navigate(
                        ProjectFileNavigationPoint(projectFile),
                        navigationOptions
                    )
                    |> ignore
            )
        ) |> ignore

type FSharpGenerateSignatureWorkflow() =
    inherit GenerateCodeWorkflowBase(
        FSharpGeneratorKinds.SignatureFile,
        PsiServicesThemedIcons.Implements.Id,
        // Seen in the dropdown menu when alt + insert is pressed.
        "Generate signature file",
        GenerateActionGroup.CLR_LANGUAGE,
        // Title of the window that opens up when the workflow is started.
        "Generate signature file",
        // Description of the window that opens up when the workflow is started.
        $"Generate a signature file for the current file.",
        FSharpGeneratorKinds.SignatureFile)

    override this.Order = 10. // See GeneratorStandardOrder.cs

[<GenerateProvider>]
type FSharpGenerateSignatureWorkflowProvider() =
    interface IGenerateImplementationsWorkflowProvider with
        member this.CreateWorkflow dataContext =
            if dataContext.IsEmpty then Seq.empty else
            let node = dataContext.GetData<IPsiSourceFile>(PsiDataConstants.SOURCE_FILE)
            if isNull node then Seq.empty else
            let solution = node.GetSolution()
            if not (solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.GenerateSignatureFile)) then
                Seq.empty
            else
                [| FSharpGenerateSignatureWorkflow() |]
