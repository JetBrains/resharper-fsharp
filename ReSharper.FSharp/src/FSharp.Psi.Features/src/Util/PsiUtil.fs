[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil

open FSharp.Compiler.Range
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.Util.Text

type IFile with
    member x.AsFSharpFile() =
        match x with
        | :? IFSharpFile as fsFile -> fsFile
        | _ -> null

type IPsiSourceFile with
    member x.FSharpFile =
        if isNull x then null else
        x.GetDominantPsiFile<FSharpLanguage>().AsFSharpFile()

type ITextControl with
    member x.GetFSharpFile(solution) =
        x.Document.GetPsiSourceFile(solution).FSharpFile

type IFSharpFile with
    member x.ParseTree =
        match x.ParseResults with
        | Some parseResults -> parseResults.ParseTree
        | _ -> None

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(document, range) =
        let offset = getStartOffset document range
        x.GetNode<'T>(DocumentOffset(document, offset))

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(range: range) =
        let document = x.GetSourceFile().Document
        x.GetNode<'T>(document, range)

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(documentOffset: DocumentOffset) =
        match x.FindTokenAt(documentOffset) with
        | null -> null
        | token -> token.GetContainingNode<'T>(true)

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(documentRange: DocumentRange) =
        x.GetNode<'T>(documentRange.StartOffset)

type IFSharpTreeNode with
    member x.FSharpLanguageService =
        x.Language.LanguageService().As<IFSharpLanguageService>()

    member x.CreateElementFactory() =
        x.FSharpLanguageService.CreateElementFactory(x.GetPsiModule())

    member x.CheckerService =
        x.FSharpFile.CheckerService
    
    member x.GetLineEnding() =
        let fsFile = x.FSharpFile
        fsFile.DetectLineEnding(fsFile.GetPsiServices()).GetPresentation()

    member x.GetIndentSize() =
        let sourceFile = x.GetSourceFile()
        sourceFile.GetFormatterSettings(x.Language).INDENT_SIZE

type FSharpLanguage with
    member x.FSharpLanguageService =
        x.LanguageService().As<IFSharpLanguageService>()        


type ITreeNode with
        member x.IsChildOf(node: ITreeNode) =
            if isNull node then false else node.Contains(x)

        member x.GetIndent(document: IDocument) =
            let startOffset = x.GetDocumentStartOffset().Offset
            let startCoords = document.GetCoordsByOffset(startOffset)
            startOffset - document.GetLineStartOffset(startCoords.Line)

        member x.Indent =
            let document = x.GetSourceFile().Document
            x.GetIndent(document)

        member x.GetStartLine(document: IDocument) =
            document.GetCoordsByOffset(x.GetDocumentStartOffset().Offset).Line

        member x.GetEndLine(document: IDocument) =
            document.GetCoordsByOffset(x.GetDocumentEndOffset().Offset).Line
        
        member x.StartLine = x.GetStartLine(x.GetSourceFile().Document)
        member x.EndLine = x.GetEndLine(x.GetSourceFile().Document)

        member x.IsSingleLine =
            let document = x.GetSourceFile().Document
            x.GetStartLine(document) = x.GetEndLine(document)

let getNode<'T when 'T :> ITreeNode and 'T : null> (fsFile: IFSharpFile) (range: DocumentRange) =
    // todo: use IExpressionSelectionProvider
    let node = fsFile.GetNode<'T>(range)
    if isNull node then failwithf "Couldn't get %O from range %O" typeof<'T>.Name range else
    node


let getParent (node: ITreeNode) =
    if isNotNull node then node.Parent else null

let getPrevSibling (node: ITreeNode) =
    if isNotNull node then node.PrevSibling else null

let getNextSibling (node: ITreeNode) =
    if isNotNull node then node.NextSibling else null


let (|TokenType|_|) tokenType (treeNode: ITreeNode) =
    if getTokenType treeNode == tokenType then Some treeNode else None

let (|Whitespace|_|) (treeNode: ITreeNode) =
    if getTokenType treeNode == FSharpTokenType.WHITESPACE then Some treeNode else None

let inline (|IgnoreParenPat|) (pat: ISynPat) = pat.IgnoreParentParens()

let inline (|IgnoreInnerParenExpr|) (expr: ISynExpr) =
    expr.IgnoreInnerParens()

let isInlineSpaceOrComment (node: ITreeNode) =
    let tokenType = getTokenType node
    tokenType == FSharpTokenType.WHITESPACE || isNotNull tokenType && tokenType.IsComment

let isInlineSpace (node: ITreeNode) =
    getTokenType node == FSharpTokenType.WHITESPACE

let isWhitespace (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && tokenType.IsWhitespace

let isFiltered (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && tokenType.IsFiltered

let isSemicolon (node: ITreeNode) =
    getTokenType node == FSharpTokenType.SEMICOLON

let isLastChild (node: ITreeNode) =
    let parent = getParent node
    isNotNull parent && parent.LastChild == node


let skipMatchingNodesAfter predicate (node: ITreeNode): ITreeNode =
    let nextSibling = node.NextSibling
    if isNull nextSibling then node else

    let rec skip (node: ITreeNode) =
        if predicate node then
            skip node.NextSibling
        else
            node

    skip nextSibling

let skipMatchingNodesBefore predicate (node: ITreeNode) =
    let prebSibling = node.PrevSibling
    if isNull prebSibling then node else

    let rec skip (node: ITreeNode) =
        if predicate node then
            skip node.PrevSibling
        else
            node
    
    skip prebSibling


let rec getLastMatchingNodeAfter predicate (node: ITreeNode) =
    let nextSibling = node.NextSibling
    if predicate nextSibling then
        getLastMatchingNodeAfter predicate nextSibling
    else
        node

let rec getFirstMatchingNodeBefore (predicate: ITreeNode -> bool) (node: ITreeNode) =
    let prevSibling = node.PrevSibling
    if predicate prevSibling then
        getFirstMatchingNodeBefore predicate prevSibling
    else
        node


let rec getThisOrNextTokenOfType tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node else

    let nextSibling = node.NextSibling
    if getTokenType nextSibling == tokenType then nextSibling else node

let rec getThisOrPrevTokenOfType tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node else

    let prevSibling = node.PrevSibling
    if getTokenType prevSibling == tokenType then prevSibling else node


let skipTokenOfTypeBefore tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node.PrevSibling
    else node

let skipTokenOfTypeAfter tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node.NextSibling
    else node


let skipNewLineBefore (node: ITreeNode) =
    skipTokenOfTypeBefore FSharpTokenType.NEW_LINE node

let skipNewLineAfter (node: ITreeNode) =
    skipTokenOfTypeAfter FSharpTokenType.NEW_LINE node


let getThisOrPrevNewLIne (node: ITreeNode) =
    getThisOrPrevTokenOfType FSharpTokenType.NEW_LINE node

let getThisOrNextNewLine (node: ITreeNode) =
    getThisOrNextTokenOfType FSharpTokenType.NEW_LINE node


let rec skipSemicolonsAndWhiteSpacesAfter node =
    let nextSibling = getNextSibling node
    let tokenType = getTokenType nextSibling
    if tokenType == FSharpTokenType.WHITESPACE ||
       tokenType == FSharpTokenType.SEMICOLON  ||
       tokenType == FSharpTokenType.SEMICOLON_SEMICOLON then
           skipSemicolonsAndWhiteSpacesAfter nextSibling
    else
        node

let isFollowedByEmptyLine (node: ITreeNode) =
    let newLine =
        node
        |> skipMatchingNodesAfter isInlineSpaceOrComment
        |> getThisOrNextNewLine

    if isNull newLine then false else

    let afterWhitespace = newLine |> skipMatchingNodesAfter isInlineSpace
    afterWhitespace != newLine && afterWhitespace :? NewLine


[<AutoOpen>]
module PsiModificationUtil =
    let replace oldChild newChild =
        ModificationUtil.ReplaceChild(oldChild, newChild) |> ignore

    let replaceWithCopy oldChild newChild =
        replace oldChild (newChild.Copy())

    let replaceWithToken oldChild (newChildTokenType: TokenNodeType) =
        replace oldChild (newChildTokenType.CreateLeafElement())

    let deleteChildRange first last =
        ModificationUtil.DeleteChildRange(first, last)

    let addNodesAfter anchor (nodes: ITreeNode list) =
        nodes |> List.fold (fun anchor treeNode ->
            ModificationUtil.AddChildAfter(anchor, treeNode)) anchor

    let addNodesBefore anchor (nodes: ITreeNode list) =
        nodes |> List.rev |> List.fold (fun anchor treeNode ->
            ModificationUtil.AddChildBefore(anchor, treeNode)) anchor

let getPrevNodeOfType nodeType (node: ITreeNode) =
    let mutable prev = node.PrevSibling
    while prev.NodeType != nodeType do
        prev <- prev.PrevSibling
    prev

let getNextNodeOfType nodeType (node: ITreeNode) =
    let mutable next = node.NextSibling
    while next.NodeType != nodeType do
        next <- next.NextSibling
    next


let rec skipIntermediateParentsOfSameType<'T when 'T :> ITreeNode> (node: 'T) =
    match node.Parent with
    | :? 'T as pat -> skipIntermediateParentsOfSameType pat
    | _ -> node

let rec skipIntermediatePatParents (pat: ISynPat) =
    skipIntermediateParentsOfSameType<ISynPat> pat


let isValid (node: ITreeNode) =
    isNotNull node && node.IsValid()


[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionSelectionProviderBase() =
    inherit ExpressionSelectionProviderBase<IFSharpTreeNode>()

    override x.IsTokenSkipped(token) =
        // todo: also ;; ?
        getTokenType token == FSharpTokenType.SEMICOLON ||
        base.IsTokenSkipped(token)


let shouldEraseSemicolon (node: ITreeNode) =
    let settingsStore = node.GetSettingsStore()
    not (settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SemicolonAtEndOfLine))


let shiftExpr shift (expr: ISynExpr) =
    for child in List.ofSeq (expr.Tokens()) do
        if not (child :? NewLine) then () else

        let nextSibling = child.NextSibling
        if nextSibling :? NewLine then () else
        if not (expr.Contains(nextSibling)) then () else

        if nextSibling :? Whitespace then
            // Skip empty lines
            if nextSibling.NextSibling.IsWhitespaceToken() then () else

            let length = nextSibling.GetTextLength() + shift
            ModificationUtil.ReplaceChild(nextSibling, Whitespace(length)) |> ignore
        else
            ModificationUtil.AddChildAfter(child, Whitespace(shift)) |> ignore
