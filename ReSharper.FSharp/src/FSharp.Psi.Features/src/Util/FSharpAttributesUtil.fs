module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

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


let addAttributesList (decl: IFSharpTreeNode) addNewLine =
    addNodesBefore decl.FirstChild [
        decl.CreateElementFactory().CreateEmptyAttributeList()
        if addNewLine then
            NewLine(decl.GetLineEnding())
            Whitespace(decl.Indent)
        else
            Whitespace()
    ] |> ignore

let getTypeDeclarationAttributeList typeDecl =
    let typeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(typeDecl)
    if typeDeclarationGroup.TypeDeclarations.[0] == typeDecl then
        let attributeLists = typeDeclarationGroup.AttributeLists
        if not attributeLists.IsEmpty then attributeLists.[0] else
        addAttributesList typeDeclarationGroup true; typeDeclarationGroup.AttributeLists.[0]
    else
        let attributeLists = typeDecl.AttributeLists
        if not attributeLists.IsEmpty then attributeLists.[0] else
        addAttributesList typeDecl false; typeDecl.AttributeLists.[0]
