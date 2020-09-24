namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeStructure

open System
open System.Collections.Generic
open System.IO
open JetBrains.Application
open JetBrains.Application.UI.Controls.JetPopupMenu
open JetBrains.Application.UI.Controls.TreeView
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeStructure
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.Icons
open JetBrains.UI.RichText
open JetBrains.Util

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
[<Language(typeof<FSharpLanguage>)>]
type FSharpCodeStructureProvider() =
    let typeExtensionIconId = compose PsiSymbolsThemedIcons.Class.Id FSharpIcons.ExtensionOverlay.Id

    let rec processNode (node: ITreeNode) (parent: CodeStructureElement) =
        InterruptableActivityCookie.CheckAndThrow()

        match node with
        | :? IFSharpFile as fsFile ->
            for decl in fsFile.ModuleDeclarations do
                processNode decl parent

        | :? IModuleLikeDeclaration as moduleLikeDeclaration ->
            let parent =
                if not (moduleLikeDeclaration :? IModuleDeclaration) then parent else
                FSharpDeclarationCodeStructureElement(parent, moduleLikeDeclaration) :> CodeStructureElement

            for memberDeclaration in moduleLikeDeclaration.Members do
                processNode memberDeclaration parent

        | :? IUnionDeclaration as unionDecl ->
            let cases = Seq.cast unionDecl.UnionCases
            processTypeDeclaration unionDecl cases parent

        | :? IUnionCaseDeclaration as caseDecl ->
            FSharpDeclarationCodeStructureElement(parent, caseDecl) |> ignore

        | :? IRecordDeclaration as recordDecl ->
            let fields = Seq.cast recordDecl.FieldDeclarations 
            processTypeDeclaration recordDecl fields parent

        | :? ITypeExtensionDeclaration as extensionDecl when not extensionDecl.IsTypePartDeclaration ->
            let parent = NamedIdentifierOwner(extensionDecl, parent, typeExtensionIconId)
            for memberDecl in  extensionDecl.TypeMembers do
                processNode memberDecl parent

        | :? IFSharpTypeDeclaration as decl ->
            processTypeDeclaration decl TreeNodeCollection.Empty parent

        | :? ITypeMemberDeclaration as typeMember ->
            FSharpDeclarationCodeStructureElement(parent, typeMember) |> ignore

        | :? IInterfaceImplementation as interfaceImpl ->
            let parent = NamedIdentifierOwner(interfaceImpl, parent, PsiSymbolsThemedIcons.Interface.Id)
            for memberDecl in interfaceImpl.TypeMembers do
                processNode memberDecl parent

        | :? ILetBindingsDeclaration as letBindings ->
            for binding in Seq.cast<ITopBinding> letBindings.Bindings do
                FSharpDeclarationCodeStructureElement(parent, binding) |> ignore

        | :? ITypeDeclarationGroup as declarationGroup ->
            for typeDeclaration in declarationGroup.TypeDeclarations do
                processNode typeDeclaration parent

        | _ -> ()

    and processTypeDeclaration (typeDecl: IFSharpTypeDeclaration) (members: IDeclaration seq) parent =
        let structureElement = FSharpDeclarationCodeStructureElement(parent, typeDecl)
        for memberDecl in members do
            processNode memberDecl structureElement

        for memberDecl in typeDecl.TypeMembers do
            processNode memberDecl structureElement

    interface IProjectFileCodeStructureProvider with
        member x.Build(sourceFile, _) =
            let fsFile = sourceFile.FSharpFile
            if isNull fsFile then null else

            let root = CodeStructureRootElement(fsFile)
            processNode fsFile root
            root


type FSharpDeclarationCodeStructureElement(parentElement, declaration: IDeclaration) =
    inherit CodeStructureElement(parentElement)

    do
        if not (isValid declaration) then
            raise (ArgumentException("declaration should be valid", "declaration"))

    let language = declaration.Language
    let declarationPointer = declaration.GetPsiServices().Pointers.CreateTreeElementPointer(declaration)
    let textRange = declaration.GetDocumentRange()
    let aspects = CodeStructureDeclarationAspects(declaration)

    let getDeclaration () = declarationPointer.GetTreeNode()

    override x.Language = language

    override x.TreeNode = getDeclaration () :> _
    override x.GetTextRange() = textRange

    override x.GetFileStructureAspect() = aspects :> _
    override x.GetGotoMemberAspect() = aspects :> _
    override x.GetMemberNavigationAspect() = aspects :> _

    override x.DumpSelf(builder: TextWriter ) =
      let description = MenuItemDescriptor(x)
      let aspect = aspects
      aspect.Present(description, PresentationState())
      builder.Write(description.Text.Text)

    interface ICodeStructureDeclarationElement with
        member x.Declaration = getDeclaration ()

        member x.DeclaredElement =
            let declaration = getDeclaration ()
            if not (isValid declaration) then null else

            let declaredElement = declaration.DeclaredElement
            if isValid declaredElement then declaredElement else null


type NameIdentifierOwnerNodeAspect(treeNode: INameIdentifierOwner, iconId: IconId) =
    static let invalidName = RichText("<Invalid>")

    let name =
        match treeNode.NameIdentifier with
        | null -> null
        | ident -> ident.Name

    let searchNames: IList<_> =
        match name with
        | null -> EmptyList.InstanceList :> _
        | name -> [| name |] :> _

    let navigationRange =
        match treeNode.NameIdentifier with
        | null -> treeNode.GetNavigationRange()
        | ident -> ident.GetNavigationRange()

    member x.Name =
        match name with
        | null -> invalidName
        | name -> RichText(name)

    interface IGotoFileMemberAspect with
        member x.Present(descriptor, _) =
            descriptor.Icon <- iconId
            descriptor.Text <- x.Name
        member x.NavigationRange = treeNode.GetNavigationRange()
        member x.GetQuickSearchTexts() = searchNames
        member x.GetSourceFile() = treeNode.GetSourceFile()

    interface IFileStructureAspect with
        member x.Present(_,item,_,_) =
            item.Images.Add(iconId)
            item.RichText <- x.Name

        member x.NavigationRange = navigationRange
        member x.InitiallyExpanded = true
        member x.GetQuickSearchTexts() = searchNames
        member x.CanMoveElements(_, _) = false
        member x.MoveElements(_, _) = raise (NotSupportedException())
        member x.CanRemove() = false
        member x.Remove() = raise (NotSupportedException())
        member x.CanRename() = false
        member x.InitialName() = raise (NotSupportedException())
        member x.Rename(_) = ()

    interface IMemberNavigationAspect with
        member x.GetNavigationRanges() = [| navigationRange |]

type NamedIdentifierOwner(treeNode: INameIdentifierOwner, parent, iconId) =
    inherit CodeStructureElement(parent)

    let aspect = NameIdentifierOwnerNodeAspect(treeNode, iconId)
    let treeNodePointer = treeNode.GetPsiServices().Pointers.CreateTreeElementPointer(treeNode)

    let textRange =
        match treeNode.NameIdentifier with
        | null -> treeNode.GetNavigationRange()
        | ident -> ident.GetDocumentRange()

    override x.TreeNode = treeNodePointer.GetTreeNode() :> _
    override x.Language = FSharpLanguage.Instance :> _
    override x.GetFileStructureAspect() = aspect :> _
    override x.GetGotoMemberAspect() = aspect :> _
    override x.GetMemberNavigationAspect() = aspect :> _
    override x.GetTextRange() = textRange
    override x.DumpSelf(writer) = writer.Write(aspect.Name)
