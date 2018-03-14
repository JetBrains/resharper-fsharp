namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeStructure

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application.UI.Icons.ComposedIcons
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
        node :? ITypeMemberDeclaration || (node :? IModuleMemberDeclaration && not (node :? IOtherMemberDeclaration)) ||
        node :? IFSharpTypeMemberDeclaration  || node :? ITopLevelModuleOrNamespaceDeclaration

    let rec processNode (node: ITreeNode) (nodeElement: CodeStructureElement) =
        for child in node.Children() do
            match child with
            | :? IDeclaration when isApplicable child ->
                processNode child (CodeStructureDeclarationElement(nodeElement, child :?> _))
            | :? IInterfaceImplementation as node ->
                processNode node (NamedTypeExpressionOwner(node, nodeElement, PsiSymbolsThemedIcons.Interface.Id))
            | :? ITypeExtension as node ->
                // todo: other type kind icons, add extension icon modificator
                processNode node (NamedTypeExpressionOwner(node, nodeElement, PsiSymbolsThemedIcons.Class.Id))
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


type NamedTypeExpressionNodeAspect([<NotNull>] treeNode: INamedTypeExpressionOwner, iconId: IconId) =
    let interfaceName =
        match treeNode.TypeExpression with
        | null -> null
        | typeName -> typeName.LongIdentifier.GetText()

    let searchNames: IList<string> =
        match interfaceName with
        | null -> EmptyList.InstanceList :> _
        | name -> [name].AsIList()

    let navigationRange =
        match treeNode.TypeExpression with
        | null -> treeNode.GetNavigationRange()
        | typeName -> typeName.LongIdentifier.GetNavigationRange()

    interface IGotoFileMemberAspect with
        member x.Present(descriptor, state) =
            descriptor.Icon <- PsiSymbolsThemedIcons.Interface.Id
            descriptor.Text <-
                match treeNode.TypeExpression with
                | null -> RichText("<Invalid>")
                | typeName -> RichText(typeName.LongIdentifier.GetText())
        member x.NavigationRange = treeNode.GetNavigationRange()
        member x.GetQuickSearchTexts() = searchNames
        member x.GetSourceFile() = treeNode.GetSourceFile()

    interface IFileStructureAspect with
        member x.Present(presenter, item, modelNode, state) =
            item.Images.Add(iconId) // 
            item.RichText <-
                match treeNode.TypeExpression with
                | null -> RichText("<Invalid>")
                | typeName -> RichText(typeName.LongIdentifier.GetText())

        member x.NavigationRange = navigationRange
        member x.InitiallyExpanded = true
        member x.GetQuickSearchTexts() = searchNames
        member x.CanMoveElements(location, dropElements) = false
        member x.MoveElements(location, dropElements) = raise (NotSupportedException())
        member x.CanRemove() = false
        member x.Remove() = raise (NotSupportedException())
        member x.CanRename() = false
        member x.InitialName() = raise (NotSupportedException())
        member x.Rename(_) = ()

    interface IMemberNavigationAspect with
        member x.GetNavigationRanges() = [| navigationRange |]

type NamedTypeExpressionOwner(treeNode: INamedTypeExpressionOwner, parent, iconId) =
    inherit CodeStructureElement(parent)

    let aspect = NamedTypeExpressionNodeAspect(treeNode, iconId)
    let treeNodePointer = treeNode.GetPsiServices().Pointers.CreateTreeElementPointer(treeNode)

    let textRange =
        match treeNode.TypeExpression with
        | null -> treeNode.GetNavigationRange().TextRange
        | typeName -> typeName.LongIdentifier.GetDocumentRange().TextRange

    override x.TreeNode = treeNodePointer.GetTreeNode() :> _
    override x.Language = FSharpLanguage.Instance :> _
    override x.GetFileStructureAspect() = aspect :> _
    override x.GetGotoMemberAspect() = aspect :> _
    override x.GetMemberNavigationAspect() = aspect :> _
    override x.GetTextRange() = textRange
    override x.DumpSelf(_) = ()
