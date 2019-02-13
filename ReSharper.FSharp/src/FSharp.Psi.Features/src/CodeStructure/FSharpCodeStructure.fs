namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeStructure

open System
open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services.CodeStructure
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
    let isApplicable (node: ITreeNode) =
        (node :? ITypeMemberDeclaration || (node :? IModuleMemberDeclaration && not (node :? IOtherMemberDeclaration)) ||
         node :? IFSharpTypeMemberDeclaration  || node :? ITopLevelModuleOrNamespaceDeclaration) &&
        not (node :? ILocalDeclaration)

    let rec processNode (node: ITreeNode) (nodeElement: CodeStructureElement) =
        for child in node.Children() do
            match child with
            | :? IDeclaration as decl when isApplicable child ->
                processNode child (CodeStructureDeclarationElement(nodeElement, decl))

            | :? IInterfaceImplementation as node ->
                processNode node (NamedIdentifierOwner(node, nodeElement, PsiSymbolsThemedIcons.Interface.Id))

            | :? ITypeExtensionDeclaration as node when not node.IsTypePartDeclaration ->
                // todo: other type kind icons, add extension icon modificator
                processNode node (NamedIdentifierOwner(node, nodeElement, PsiSymbolsThemedIcons.Class.Id))

            | _ -> ()

    interface IPsiFileCodeStructureProvider with
        member x.Build(file, _) =
            match file with
            | :? IFSharpFile ->
                let root = FSharpCodeStructureRootElement(file)
                processNode file (root :> CodeStructureElement)
                root :> _
            | _ -> null


type FSharpCodeStructureRootElement(file) =
    inherit CodeStructureRootElement(file)


type NamedTypeExpressionNodeAspect(treeNode: INameIdentifierOwner, iconId: IconId) =
    let interfaceName =
        match treeNode.NameIdentifier with
        | null -> null
        | ident -> ident.Name

    let searchNames: IList<string> =
        match interfaceName with
        | null -> EmptyList.InstanceList :> _
        | name -> [| name |] :> _

    let navigationRange =
        match treeNode.NameIdentifier with
        | null -> treeNode.GetNavigationRange()
        | ident -> ident.GetNavigationRange()

    member x.Name =
        match interfaceName with
        | null -> RichText("<Invalid>")
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

    let aspect = NamedTypeExpressionNodeAspect(treeNode, iconId)
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
    override x.DumpSelf(_) = ()
