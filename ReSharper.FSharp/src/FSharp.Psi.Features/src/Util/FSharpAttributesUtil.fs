module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

open System
open JetBrains.Diagnostics
open JetBrains.Metadata.Reader.API
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.Util.Extension

let addAttribute (attributeList: IAttributeList) (attribute: IAttribute) =
    let attribute = ModificationUtil.AddChildAfter(attributeList.LBrack, attribute)

    if attributeList.Attributes.Count > 1 then
        addNodesAfter attribute [
            FSharpTokenType.SEMICOLON.CreateLeafElement()
            if not (isWhitespace attribute.NextSibling) then 
                Whitespace()
        ] |> ignore


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
        Whitespace()
        attribute

        let nextSiblingType = getTokenType anchor.NextSibling
        if isNotNull nextSiblingType &&
                not (nextSiblingType.IsWhitespace || nextSiblingType == FSharpTokenType.GREATER_RBRACK) then
            Whitespace()
    ] |> ignore


let removeAttributeList (attrList: IAttributeList) =
    if isOnlyMeaningfulNodeOnLine attrList then
        let first = getFirstMatchingNodeBefore isInlineSpaceOrComment attrList
        let last =
            match attrList.PrevSibling with
            | null -> getLastInlineSpaceOrCommentSkipNewLine attrList
            | _ -> skipMatchingNodesAfter isInlineSpaceOrComment attrList
        deleteChildRange first last

    elif isFirstMeaningfulNodeOnLine attrList then
        let last =
            match attrList.PrevSibling with
            | null -> getLastInlineSpaceOrCommentSkipNewLine attrList
            | _ -> getLastMatchingNodeAfter isInlineSpaceOrComment attrList
        deleteChildRange attrList last

    else
        let first = getFirstMatchingNodeBefore isInlineSpaceOrComment attrList
        let last = getLastMatchingNodeAfter isInlineSpaceOrComment attrList

        if isLastMeaningfulNodeOnLine attrList then
            deleteChildRange first last
        else
            replaceRangeWithNode first last (Whitespace())

let removeAttributeFromList (attr: IAttribute) =
    let attrList = AttributeListNavigator.GetByAttribute(attr)
    let attrs = attrList.Attributes
    let rBrack = attrList.RBrack

    if isNotNull rBrack && attrs.Count = 1 then
        deleteChildRange attrList.LBrack.NextSibling rBrack.PrevSibling else

    if attrs.Count > 1 && attrs.First() == attr then
        deleteChildRange attrList.LBrack.NextSibling attrs.[1].PrevSibling else

    if isNotNull rBrack && attrs.Last() == attr then
        deleteChildRange attrs.[attrs.Count - 2].NextSibling rBrack.PrevSibling else

    let isFirstOnLine = isFirstMeaningfulNodeOnLine attr

    let first =
        if not isFirstOnLine then getFirstMatchingNodeBefore isInlineSpaceOrComment attr else attr :> _

    let last =
        let nodeAfter = skipMatchingNodesAfter isWhitespaceOrComment attr
        let last =
            if getTokenType nodeAfter == FSharpTokenType.SEMICOLON then
                getLastMatchingNodeAfter isInlineSpaceOrComment nodeAfter
            else
                getLastMatchingNodeAfter isInlineSpaceOrComment attr

        if not isFirstOnLine then last else
        getThisOrNextNewLine last |> getLastMatchingNodeAfter isInlineSpaceOrComment

    if isFirstOnLine then
        deleteChildRange first last
    else
        replaceRangeWithNode first last (Whitespace())


let removeAttributeOrList (attr: IAttribute) =
    let attrList = AttributeListNavigator.GetByAttribute(attr).NotNull()
    if attrList.Attributes.Count = 1 then
        removeAttributeList attrList
    else
        removeAttributeFromList attr


let addOuterAttributeListWithIndent addNewLine (indent: int) (decl: IFSharpTreeNode) =
    addNodesBefore decl.FirstChild [
        decl.CreateElementFactory().CreateEmptyAttributeList()
        if addNewLine then
            NewLine(decl.GetLineEnding())
            Whitespace(indent)
        else
            Whitespace()
    ] |> ignore

let addOuterAttributeList addNewLine (decl: IFSharpTreeNode) =
    addOuterAttributeListWithIndent addNewLine decl.Indent decl

let addAttributeList (decl: IFSharpTypeOrExtensionDeclaration) =
    addNodesAfter decl.TypeKeyword [
        Whitespace()
        decl.CreateElementFactory().CreateEmptyAttributeList()
    ] |> ignore


let isOuterAttributeList (typeDecl: IFSharpTypeOrExtensionDeclaration) (attrList: IAttributeList) =
    attrList.GetTreeStartOffset().Offset < typeDecl.TypeKeyword.GetTreeStartOffset().Offset

let getTypeDeclarationAttributeList (typeDecl: #IFSharpTypeOrExtensionDeclaration) =
    if typeDecl.IsPrimary then
        let attributeLists = typeDecl.AttributeLists
        if attributeLists.Count > 0 && isOuterAttributeList typeDecl attributeLists.[0] then
            attributeLists.[0]
        else
            addOuterAttributeList true typeDecl
            typeDecl.AttributeLists.[0]
    else
        let attributeLists = typeDecl.AttributeLists
        if attributeLists.IsEmpty then
            addAttributeList typeDecl
        typeDecl.AttributeLists.[0]

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
