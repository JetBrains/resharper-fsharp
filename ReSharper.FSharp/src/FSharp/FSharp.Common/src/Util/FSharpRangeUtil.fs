[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpRangeUtil

open FSharp.Compiler.Text
open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.ReSharper.Psi
open JetBrains.Util

// FCS lines are 1-based.

[<Extension; CompiledName("ToDocumentCoords")>]
let getDocumentCoords (pos: Position) =
    DocumentCoords(docLine (pos.Line - 1), docColumn pos.Column)

[<Extension; CompiledName("GetDocumentOffset")>]
let getDocumentOffset ([<NotNull>] document: IDocument) (coords: DocumentCoords) =
    if document.GetLineLength(coords.Line) >= coords.Column then
        document.GetDocumentOffsetByCoords(coords)
    else
        document.GetLineEndDocumentOffsetNoLineBreak(coords.Line)

[<Extension; CompiledName("GetTreeOffset")>]
let getTreeOffsetByCoords ([<NotNull>] document: IDocument) (coords: DocumentCoords) =
    if document.GetLineLength(coords.Line) >= coords.Column then
        TreeOffset(document.GetOffsetByCoords(coords))
    else
        TreeOffset.InvalidOffset

[<Extension; CompiledName("GetTreeOffset")>]
let getTreeOffsetByPos ([<NotNull>] document) pos =
    let coords = getDocumentCoords pos
    getTreeOffsetByCoords document coords

[<Extension; CompiledName("GetTreeStartOffset")>]
let getTreeStartOffset ([<NotNull>] document) (range: range) =
    getTreeOffsetByPos document range.Start

[<Extension; CompiledName("GetTreeEndOffset")>]
let getTreeEndOffset ([<NotNull>] document) (range: range) =
    getTreeOffsetByPos document range.End

[<Extension; CompiledName("GetTreeTextRange")>]
let getTreeTextRange (document: IDocument) (range: range) =
    let startOffset = getTreeStartOffset document range
    let endOffset = getTreeEndOffset document range
    TreeTextRange(startOffset, endOffset)


[<Extension; CompiledName("ToPos")>]
let getPosFromCoords (coords: DocumentCoords) =
    let line = int coords.Line + 1
    let column = int coords.Column
    Position.mkPos line column

[<Extension; CompiledName("GetPos")>]
let getPosFromDocumentOffset (offset: DocumentOffset) =
    let coords = offset.ToDocumentCoords()
    getPosFromCoords coords

[<Extension; CompiledName("GetOffset")>]
let getPosOffset ([<NotNull>] document) pos =
    let coords = getDocumentCoords pos
    let offset = getDocumentOffset document coords
    offset.Offset

[<Extension; CompiledName("GetStartOffset")>]
let getStartOffset ([<NotNull>] document) (range: range) =
    getPosOffset document range.Start

[<Extension; CompiledName("GetEndOffset")>]
let getEndOffset ([<NotNull>] document) (range: range) =
    getPosOffset document range.End

[<Extension; CompiledName("ToFcsRange")>]
let ofFileDocumentRange (documentRange: DocumentRange) (path: VirtualFileSystemPath) =
    let startPos = getPosFromDocumentOffset documentRange.StartOffset
    let endPos = getPosFromDocumentOffset documentRange.EndOffset
    Range.mkRange path.FullPath startPos endPos

[<Extension; CompiledName("ToFcsRange")>]
let ofDocumentRange (documentRange: DocumentRange) =
    ofFileDocumentRange documentRange (VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext))

[<Extension; CompiledName("GetTextRange")>]
let getTextRange (document: IDocument) (range: range) =
    let startOffset = getStartOffset document range
    let endOffset = getEndOffset document range
    TextRange(startOffset, endOffset)

[<Extension; CompiledName("GetDocumentRange")>]
let getDocumentRange (document: IDocument) (range: range) =
    let textRange = getTextRange document range
    DocumentRange(document, textRange)
