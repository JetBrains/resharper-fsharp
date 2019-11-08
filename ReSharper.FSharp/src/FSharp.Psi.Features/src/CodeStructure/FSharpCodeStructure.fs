namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeStructure

open System
open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services.CodeStructure
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.UI.Icons
open JetBrains.Util
open JetBrains.UI.RichText

[<Language(typeof<FSharpLanguage>)>]
type FSharpCodeStructureProvider() =
    let typeExtensionIconId = compose PsiSymbolsThemedIcons.Class.Id FSharpIcons.ExtensionOverlay.Id

    let rec processNode (node: ITreeNode) (parent: CodeStructureElement) =
        match node with
        | :? IFSharpFile as fsFile ->
            for decl in fsFile.ModuleDeclarations do
                processNode decl parent

        | :? IModuleLikeDeclaration as moduleLikeDeclaration ->
            let parent =
                if not (moduleLikeDeclaration :? IModuleDeclaration) then parent else
                CodeStructureDeclarationElement(parent, moduleLikeDeclaration) :> CodeStructureElement

            for memberDeclaration in moduleLikeDeclaration.Members do
                processNode memberDeclaration parent

        | :? IUnionDeclaration as unionDecl ->
            let cases = Seq.cast unionDecl.UnionCases
            processTypeDeclaration unionDecl cases parent

        | :? IUnionCaseDeclaration as caseDecl ->
            CodeStructureDeclarationElement(parent, caseDecl) |> ignore

        | :? IRecordDeclaration as recordDecl ->
            let fields = Seq.cast recordDecl.Fields 
            processTypeDeclaration recordDecl fields parent

        | :? ITypeExtensionDeclaration as extensionDecl when not extensionDecl.IsTypePartDeclaration ->
            let parent = NamedIdentifierOwner(extensionDecl, parent, typeExtensionIconId)
            for memberDecl in  extensionDecl.TypeMembers do
                processNode memberDecl parent

        | :? IFSharpTypeDeclaration as decl ->
            processTypeDeclaration decl TreeNodeCollection.Empty parent

        | :? ITypeMemberDeclaration as typeMember ->
            CodeStructureDeclarationElement(parent, typeMember) |> ignore

        | :? IInterfaceImplementation as interfaceImpl ->
            let parent = NamedIdentifierOwner(interfaceImpl, parent, PsiSymbolsThemedIcons.Interface.Id)
            for memberDecl in interfaceImpl.TypeMembers do
                processNode memberDecl parent

        | :? ILetModuleDecl as letDecl ->
            for binding in Seq.cast<ITopBinding> letDecl.Bindings do
                CodeStructureDeclarationElement(parent, binding) |> ignore

        | _ -> ()

    and processTypeDeclaration (typeDecl: IFSharpTypeDeclaration) (members: IDeclaration seq) parent =
        let structureElement = CodeStructureDeclarationElement(parent, typeDecl)
        for memberDecl in members do
            processNode memberDecl structureElement

        for memberDecl in typeDecl.TypeMembers do
            processNode memberDecl structureElement

    interface IPsiFileCodeStructureProvider with
        member x.Build(file, _) =
            match file.As<IFSharpFile>() with
            | null -> null
            | fsFile ->

            let root = CodeStructureRootElement(fsFile)
            processNode fsFile root
            root


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
        | null -> treeNode.GetNavigationRange().TextRange
        | ident -> ident.GetDocumentRange().TextRange

    override x.TreeNode = treeNodePointer.GetTreeNode() :> _
    override x.Language = FSharpLanguage.Instance :> _
    override x.GetFileStructureAspect() = aspect :> _
    override x.GetGotoMemberAspect() = aspect :> _
    override x.GetMemberNavigationAspect() = aspect :> _
    override x.GetTextRange() = textRange
    override x.DumpSelf(writer) = writer.Write(aspect.Name)
