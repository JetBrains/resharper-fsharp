module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
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
