[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil

open FSharp.Compiler.Range
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
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


let getTokenType (node: ITreeNode) =
    if isNotNull node then node.GetTokenType() else null

let (|TokenType|_|) tokenType (treeNode: ITreeNode) =
    if getTokenType treeNode == tokenType then Some treeNode else None

let (|Whitespace|_|) (treeNode: ITreeNode) =
    if getTokenType treeNode == FSharpTokenType.WHITESPACE then Some treeNode else None

let (|IgnoreParenPat|) (pat: ISynPat) = pat.IgnoreParentParens()

let (|IgnoreInnerParenExpr|) (expr: ISynExpr) =
    expr.IgnoreInnerParens()

let isWhitespace (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && tokenType.IsWhitespace

let isSemicolon (node: ITreeNode) =
    getTokenType node == FSharpTokenType.SEMICOLON


let rec skipTokensOfTypeAfter tokenType (node: ITreeNode) =
    let nextSibling = node.NextSibling
    if getTokenType nextSibling == tokenType then
        skipTokensOfTypeAfter tokenType nextSibling
    else
        node

let rec skipTokensOfTypeBefore tokenType (node: ITreeNode) =
    let prevSibling = node.PrevSibling
    if getTokenType prevSibling == tokenType then
        skipTokensOfTypeBefore tokenType prevSibling
    else
        node


let rec skipOneTokenOfTypeAfter tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node else

    let nextSibling = node.NextSibling
    if getTokenType nextSibling == tokenType then nextSibling else node

let rec skipOneTokenOfTypeBefore tokenType (node: ITreeNode) =
    if getTokenType node == tokenType then node else

    let prevSibling = node.PrevSibling
    if getTokenType prevSibling == tokenType then prevSibling else node


let getRangeEndWithSpaceBefore (node: ITreeNode) =
    let prevSibling = node.PrevSibling
    if not (getTokenType prevSibling == FSharpTokenType.WHITESPACE) then node else

    skipTokensOfTypeAfter FSharpTokenType.WHITESPACE prevSibling

let getRangeEndWithSpaceAfter (node: ITreeNode) =
    let nextSibling = node.NextSibling
    if not (getTokenType nextSibling == FSharpTokenType.WHITESPACE) then node else

    skipTokensOfTypeAfter FSharpTokenType.WHITESPACE nextSibling

let getRangeEndWithNewLineAfter (node: ITreeNode) =
    let nextSibling = node.NextSibling
    if not (isWhitespace nextSibling) then node else

    let last = skipTokensOfTypeAfter FSharpTokenType.WHITESPACE nextSibling
    skipOneTokenOfTypeAfter FSharpTokenType.NEW_LINE last

let getRangeStartWithNewLineBefore (node: ITreeNode) =
    let prevSibling = node.PrevSibling
    if not (isWhitespace prevSibling) then node else

    let last = skipTokensOfTypeBefore FSharpTokenType.WHITESPACE prevSibling
    skipOneTokenOfTypeBefore FSharpTokenType.NEW_LINE last


let getRangeWithNewLineAfter (node: ITreeNode) =
    TreeRange(node, getRangeEndWithNewLineAfter node)

let getRangeWithNewLineBefore (node: ITreeNode) =
    TreeRange(getRangeStartWithNewLineBefore node, node)


let shouldEraseSemicolon (node: ITreeNode) =
    let settingsStore = node.GetSettingsStore()
    not (settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SemicolonAtEndOfLine))

let rec skipThisWhitespaceBeforeNode node =
    let rec skip seenSemicolon node =
        if isWhitespace node then
            skip seenSemicolon node.PrevSibling
        elif not seenSemicolon && isSemicolon node && shouldEraseSemicolon node then
            skip true node.PrevSibling
        else
            node
    skip false node

let rec skipPreviousWhitespaceBeforeNode node =
    let rec skip seenSemicolon (node: ITreeNode) =
        let prevSibling = node.PrevSibling
        if isWhitespace prevSibling then
            skip seenSemicolon prevSibling
        elif not seenSemicolon && isSemicolon prevSibling && shouldEraseSemicolon node then
            skip true prevSibling
        else
            node
    skip false node


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
    inherit ExpressionSelectionProviderBase<ISynExpr>()

    override x.IsTokenSkipped(token) =
        // todo: also ;; ?
        getTokenType token == FSharpTokenType.SEMICOLON ||
        base.IsTokenSkipped(token)
