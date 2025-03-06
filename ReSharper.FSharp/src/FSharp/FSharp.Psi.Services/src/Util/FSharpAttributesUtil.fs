module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

open System
open JetBrains.Diagnostics
open JetBrains.Metadata.Reader.API
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util.Extension

let addAttribute (attributeList: IAttributeList) (attribute: IAttribute) =
    let attribute = ModificationUtil.AddChildAfter(attributeList.LBrack, attribute)

    if attributeList.Attributes.Count > 1 then
        ModificationUtil.AddChildAfter(attribute, FSharpTokenType.SEMICOLON.CreateLeafElement()) |> ignore

    attribute


let addAttributeAfter (anchor: IAttribute) (attribute: IAttribute) =
    let anchor, seenSemi =
        let node = skipMatchingNodesAfter isInlineSpaceOrComment anchor
        if getTokenType node == FSharpTokenType.SEMICOLON then
            node, true
        else
            anchor :> _, false

    addNodesAfter anchor [
        if not seenSemi then
            FSharpTokenType.SEMICOLON.CreateLeafElement()
        attribute
    ] |> ignore


let removeAttributeList (attrList: IAttributeList) =
    let last = getLastMatchingNodeAfter isInlineSpaceOrComment attrList
    ModificationUtil.DeleteChildRange(attrList, last)

let removeAttributeFromList (attr: IAttribute) =
    let attrs = AttributeListNavigator.GetByAttribute(attr).Attributes
    Assertion.Assert(attrs.Count > 1)

    let isLast = attrs.Last() == attr

    let first: ITreeNode =
        if not isLast then attr else

        let nodeBefore = skipMatchingNodesBefore isInlineSpaceOrComment attr
        if getTokenType nodeBefore == FSharpTokenType.SEMICOLON then nodeBefore else attr

    let last =
        let nodeAfter = skipMatchingNodesAfter isWhitespaceOrComment attr
        if getTokenType nodeAfter == FSharpTokenType.SEMICOLON then
            getLastMatchingNodeAfter isInlineSpaceOrComment nodeAfter
        else
            getLastMatchingNodeAfter isInlineSpaceOrComment attr

    ModificationUtil.DeleteChildRange(first, last)


let removeAttributeOrList (attr: IAttribute) =
    let attrList = AttributeListNavigator.GetByAttribute(attr).NotNull()
    if attrList.Attributes.Count = 1 then
        removeAttributeList attrList
    else
        removeAttributeFromList attr


let addAttributeListWithIndent (anchor: ITreeNode) =
    let attrList = anchor.CreateElementFactory().CreateEmptyAttributeList()
    addNodeBefore anchor attrList

let addOuterAttributeList (decl: ITreeNode) =
    addAttributeListWithIndent decl.FirstChild

let addAttributeList (anchor: ITreeNode) =
    addAttributeListWithIndent anchor


let addAttributeListToTypeDeclaration (decl: IFSharpTypeOrExtensionDeclaration) =
    let attrList = decl.CreateElementFactory().CreateEmptyAttributeList()
    addNodeAfter decl.TypeKeyword attrList

let addAttributeListToLetBinding newLine (binding: IBinding) =
    if newLine then
        addOuterAttributeList binding
    else
        let inlineKeyword = binding.InlineKeyword
        if isNotNull inlineKeyword then
            addAttributeList inlineKeyword else

        let mutableKeyword = binding.MutableKeyword
        if isNotNull mutableKeyword then
            addAttributeList mutableKeyword else

        let headPattern = binding.HeadPattern
        if isNotNull headPattern then
            addAttributeList headPattern else

        addOuterAttributeList binding


let isOuterAttributeList (typeDecl: IFSharpTypeOrExtensionDeclaration) (attrList: IAttributeList) =
    attrList.GetTreeStartOffset().Offset < typeDecl.TypeKeyword.GetTreeStartOffset().Offset

let getTypeDeclarationAttributeList (typeDecl: #IFSharpTypeOrExtensionDeclaration) =
    if typeDecl.IsPrimary then
        let attributeLists = typeDecl.AttributeLists
        if attributeLists.Count > 0 && isOuterAttributeList typeDecl attributeLists[0] then
            attributeLists[0]
        else
            addOuterAttributeList typeDecl
            typeDecl.AttributeLists[0]
    else
        let attributeLists = typeDecl.AttributeLists
        if attributeLists.IsEmpty then
            addAttributeListToTypeDeclaration typeDecl
        typeDecl.AttributeLists[0]

let resolvesToType (clrTypeName: IClrTypeName) (attr: IAttribute) =
    let reference = attr.ReferenceName.Reference
    if isNull reference then false else

    // todo: we should also account type abbreviations in future
    let referenceName = reference.GetName()
    let shortName = clrTypeName.ShortName

    if not (startsWith referenceName shortName) then false else
    if referenceName.Length <> shortName.Length &&
            referenceName <> shortName.SubstringBeforeLast("Attribute", StringComparison.Ordinal) then false else

    let declaredElement = reference.Resolve().DeclaredElement
    let typeElement = declaredElement.As<ITypeElement>()
    isNotNull typeElement && typeElement.GetClrName() = clrTypeName
