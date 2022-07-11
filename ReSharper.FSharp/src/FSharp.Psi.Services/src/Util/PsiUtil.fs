[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil

open FSharp.Compiler.Text
open JetBrains.Annotations
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
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.TextControl
open JetBrains.Util
open JetBrains.Util.Text

type IFile with
    member x.AsFSharpFile() =
        match x with
        | :? IFSharpFile as fsFile -> fsFile
        | _ -> null

type IPsiSourceFile with
    member x.FSharpFile =
        if isNull x then null else
        x.GetPrimaryPsiFile().AsFSharpFile()

type ITextControl with
    member x.GetFSharpFile(solution) =
        x.Document.GetPsiSourceFile(solution).FSharpFile

type IFile with
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


type FSharpLanguage with
    member x.FSharpLanguageService =
        x.LanguageService().As<IFSharpLanguageService>()


type ITreeNode with
    member x.GetLineEnding() =
        let fsFile = x.GetContainingFile()
        fsFile.DetectLineEnding(fsFile.GetPsiServices()).GetPresentation()

    member x.GetIndentSize() =
        let sourceFile = x.GetSourceFile()
        sourceFile.GetFormatterSettings(x.Language).INDENT_SIZE

    member x.IsChildOf(node: ITreeNode) =
        if isNull node then false else node.Contains(x)

    member x.GetIndent(document: IDocument) =
        let startOffset = x.GetDocumentStartOffset().Offset
        let startCoords = document.GetCoordsByOffset(startOffset)
        startOffset - document.GetLineStartOffset(startCoords.Line)

    member x.Indent =
        match x.GetSourceFile() with
        | null -> FormatterHelper.CalcNodeIndent(x, x.GetCodeFormatter()).Length
        | sourceFile -> x.GetIndent(sourceFile.Document)

    member x.GetStartLine(document: IDocument) =
        document.GetCoordsByOffset(x.GetDocumentStartOffset().Offset).Line

    member x.GetEndLine(document: IDocument) =
        document.GetCoordsByOffset(x.GetDocumentEndOffset().Offset).Line

    member x.StartLine = x.GetStartLine(x.GetSourceFile().Document)
    member x.EndLine = x.GetEndLine(x.GetSourceFile().Document)

    member x.IsSingleLine =
        let document = x.GetSourceFile().Document
        x.GetStartLine(document) = x.GetEndLine(document)

let getNode<'T when 'T :> ITreeNode and 'T: not struct and 'T: null> (fsFile: IFSharpFile) (range: DocumentRange) =
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

let inline (|IgnoreParenPat|) (fsPattern: IFSharpPattern) =
    fsPattern.IgnoreParentParens()

let inline (|IgnoreInnerParenExpr|) (expr: IFSharpExpression) =
    expr.IgnoreInnerParens()

let isInlineSpaceOrComment (node: ITreeNode) =
    let tokenType = getTokenType node
    tokenType == FSharpTokenType.WHITESPACE || isNotNull tokenType && tokenType.IsComment // todo: multiline comments?

let isInlineSpace (node: ITreeNode) =
    getTokenType node == FSharpTokenType.WHITESPACE

let isNewLine (node: ITreeNode) =
    getTokenType node == FSharpTokenType.NEW_LINE

let isWhitespace (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && tokenType.IsWhitespace

let isWhitespaceOrComment (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && (tokenType.IsWhitespace || tokenType.IsComment)

let isFiltered (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && tokenType.IsFiltered

let isSemicolon (node: ITreeNode) =
    getTokenType node == FSharpTokenType.SEMICOLON

let isIdentifierOrKeyword (node: ITreeNode) =
    let tokenType = getTokenType node
    isNotNull tokenType && (tokenType.IsIdentifier || tokenType.IsKeyword)

let isFirstChild (node: ITreeNode) =
    let parent = getParent node
    isNotNull parent && parent.FirstChild == node

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
    let prevSibling = node.PrevSibling
    if isNull prevSibling then node else

    let rec skip (node: ITreeNode) =
        if predicate node then
            skip node.PrevSibling
        else
            node

    skip prevSibling


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

let isFollowedByEmptyLineOrComment (node: ITreeNode) =
    let newLine =
        node
        |> skipMatchingNodesAfter isInlineSpaceOrComment
        |> getThisOrNextNewLine

    if isNull newLine then false else

    let afterWhitespace = newLine |> skipMatchingNodesAfter isInlineSpace
    afterWhitespace != newLine && afterWhitespace :? NewLine

let isAfterEmptyLine (node: ITreeNode) =
    let prevNonWhitespace = skipMatchingNodesBefore isInlineSpace node
    let prevPrevNonWhiteSpace = skipMatchingNodesBefore isInlineSpace prevNonWhitespace

    (prevNonWhitespace != prevPrevNonWhiteSpace || isNotNull prevNonWhitespace && isNull prevNonWhitespace.PrevSibling) &&
    prevNonWhitespace :? NewLine && (isNull prevPrevNonWhiteSpace || prevPrevNonWhiteSpace :? NewLine)

let isBeforeEmptyLine (node: ITreeNode) =
    let nextNonWhitespace = skipMatchingNodesAfter isInlineSpace node
    let nextNextNonWhiteSpace = skipMatchingNodesAfter isInlineSpace nextNonWhitespace

    (nextNonWhitespace != nextNextNonWhiteSpace || isNotNull nextNonWhitespace && isNull nextNonWhitespace.NextSibling) &&
    nextNonWhitespace :? NewLine && (isNull nextNextNonWhiteSpace || nextNextNonWhiteSpace :? NewLine)

let isFirstChildOrAfterEmptyLine (node: ITreeNode) =
    isNull node.PrevSibling || isAfterEmptyLine node

let isAtEmptyLine (node: ITreeNode) =
    isInlineSpace node &&
    isNewLine (skipMatchingNodesBefore isInlineSpace node) &&
    isNewLine (skipMatchingNodesAfter isInlineSpace node) ||

    isNewLine node && isNewLine node.NextSibling

let isNullOrNewLine (node: ITreeNode) =
    isNull node || isNewLine node

let getLastMatchingNodeAfterSkippingToken predicate tokenType node =
    node
    |> getLastMatchingNodeAfter predicate
    |> getThisOrNextTokenOfType tokenType
    |> getLastMatchingNodeAfter predicate

let getLastInlineSpaceOrCommentSkipNewLine (node: ITreeNode) =
    getLastMatchingNodeAfterSkippingToken isInlineSpaceOrComment FSharpTokenType.NEW_LINE node


/// Only takes siblings into account.
let isFirstMeaningfulNodeOnLine (node: ITreeNode) =
    let skipBefore = getFirstMatchingNodeBefore isInlineSpaceOrComment node
    let newLine = getThisOrPrevNewLIne skipBefore
    newLine == node && isNull node.PrevSibling || isNewLine newLine

/// Only takes siblings into account.
let isLastMeaningfulNodeOnLine (node: ITreeNode) =
    let skipAfter = getLastMatchingNodeAfter isInlineSpaceOrComment node
    let newLine = getThisOrNextNewLine skipAfter
    newLine == node && isNull node.NextSibling || isNullOrNewLine newLine

/// Only takes siblings into account.
let isOnlyMeaningfulNodeOnLine (node: ITreeNode) =
    isFirstMeaningfulNodeOnLine node && isLastMeaningfulNodeOnLine node


[<AutoOpen>]
module PsiModificationUtil =
    /// Wraps ModificationUtil.ReplaceChild and ignores the resulting replaced node.
    /// Use ModificationUtil.ReplaceChild if resulting node is needed.
    ///
    /// Warning: newChild should not be child of oldChild.
    let replace oldChild newChild =
        ModificationUtil.ReplaceChild(oldChild, newChild) |> ignore

    /// Wraps ModificationUtil.ReplaceChild and ignores the resulting replaced node.
    /// Use ModificationUtil.ReplaceChild if resulting node is needed.
    ///
    /// Should be used when newChild is a child of oldChild.
    let replaceWithCopy oldChild newChild =
        replace oldChild (newChild.Copy())

    /// A shorthand helper for PsiModificationUtil.replace.
    let replaceWithToken oldChild (newChildTokenType: TokenNodeType) =
        replace oldChild (newChildTokenType.CreateLeafElement())

    let replaceWithNodeKeepChildren (oldChild: ITreeNode) (newChildNodeType: CompositeNodeType) =
        use disableFormatter = new DisableCodeFormatter()

        let newNode = ModificationUtil.ReplaceChild(oldChild, newChildNodeType.Create())
        LowLevelModificationUtil.AddChild(newNode, oldChild.Children().AsArray())

        newNode

    let deleteChildRange first last =
        ModificationUtil.DeleteChildRange(first, last)

    let deleteChild child =
        ModificationUtil.DeleteChild(child)

    let replaceRangeWithNode first last replaceNode =
        ModificationUtil.ReplaceChildRange(TreeRange(first, last), TreeRange(replaceNode)) |> ignore

    let addNodesAfter anchor (nodes: ITreeNode seq) =
        nodes |> Seq.fold (fun anchor treeNode ->
            ModificationUtil.AddChildAfter(anchor, treeNode)) anchor

    let addNodesBefore anchor (nodes: ITreeNode seq) =
        nodes |> Seq.rev |> Seq.fold (fun anchor treeNode ->
            ModificationUtil.AddChildBefore(anchor, treeNode)) anchor

    let addNodeBefore anchor node = ModificationUtil.AddChildBefore(anchor, node) |> ignore
    let addNodeAfter anchor node = ModificationUtil.AddChildAfter(anchor, node) |> ignore

    let moveToNewLine lineEnding (indent: int) (node: ITreeNode) =
        let prevSibling = node.PrevSibling
        if isInlineSpace prevSibling then
            ModificationUtil.DeleteChild(prevSibling)

        addNodesBefore node [
            NewLine(lineEnding)
            Whitespace(indent)
        ] |> ignore

    let removeModuleMember (moduleMember: IModuleMember) =
        let first = getFirstMatchingNodeBefore isInlineSpaceOrComment moduleMember
        let last =
            moduleMember
            |> skipSemicolonsAndWhiteSpacesAfter
            |> getThisOrNextNewLine

        deleteChildRange first last

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


let rec skipIntermediateParentsOfSameType<'T when 'T :> ITreeNode and 'T: not struct> (node: 'T) =
    if isNull node then node else

    match node.Parent with
    | :? 'T as pat -> skipIntermediateParentsOfSameType pat
    | _ -> node

let rec skipIntermediatePatParents (fsPattern: IFSharpPattern) =
    skipIntermediateParentsOfSameType<IFSharpPattern> fsPattern


[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionSelectionProvider() =
    inherit ExpressionSelectionProviderBase<IFSharpExpression>()

    override x.IsTokenSkipped(token) =
        // todo: also ;; ?
        getTokenType token == FSharpTokenType.SEMICOLON ||
        base.IsTokenSkipped(token)


type FSharpTreeNodeSelectionProvider() =
    inherit ExpressionSelectionProviderBase<IFSharpTreeNode>()

    static member val Instance = FSharpTreeNodeSelectionProvider()


let shouldEraseSemicolon (node: ITreeNode) =
    let settingsStore = node.GetSettingsStoreWithEditorConfig()
    not (settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SemicolonAtEndOfLine))

let shiftWhitespaceBefore shift (whitespace: Whitespace) =
    let length = whitespace.GetTextLength() + shift
    if length > 0 then
        ModificationUtil.ReplaceChild(whitespace, Whitespace(length)) |> ignore
    else
        ModificationUtil.DeleteChild(whitespace)

let shiftNode shift (expr: #IFSharpTreeNode) =
    if shift = 0 || isNull expr then () else

    for child in List.ofSeq (expr.Tokens()) do
        if not (child :? NewLine) then () else

        let nextSibling = child.NextSibling
        if not (expr.Contains(nextSibling)) then () else

        match nextSibling with
        | :? NewLine -> ()
        | :? Whitespace as whitespace ->
            // Skip empty lines
            if not (whitespace.NextSibling.IsWhitespaceToken()) then
                shiftWhitespaceBefore shift whitespace
        | _ ->
            if shift > 0 then
                ModificationUtil.AddChildAfter(child, Whitespace(shift)) |> ignore

let shiftWithWhitespaceBefore shift (expr: IFSharpExpression) =
    match expr.PrevSibling with
    | :? Whitespace as whitespace ->
        if not (whitespace.NextSibling.IsWhitespaceToken()) then
            shiftWhitespaceBefore shift whitespace
    | _ ->
        if shift > 0 then
            ModificationUtil.AddChildBefore(expr, Whitespace(shift)) |> ignore

    shiftNode shift expr


let withNewLineAndIndentBefore (indent: int) (node: IFSharpTreeNode) =
    [ NewLine(node.GetLineEnding()) :> ITreeNode
      Whitespace(indent) :> _
      node :> _ ]


[<CanBeNull>]
let rec getOutermostPrefixAppExpr ([<CanBeNull>] expr: IFSharpExpression) =
    let prefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr.IgnoreParentParens())
    if isNull prefixAppExpr || isNull prefixAppExpr.ArgumentExpression then expr else

    getOutermostPrefixAppExpr prefixAppExpr

let rec getPrefixAppExprArgs (expr: IFSharpExpression) =
    let mutable currentExpr = expr
    seq {
        while isNotNull currentExpr do
            let prefixApp = PrefixAppExprNavigator.GetByFunctionExpression(currentExpr.IgnoreParentParens())
            if isNotNull prefixApp && isNotNull prefixApp.ArgumentExpression then
                currentExpr <- prefixApp
                yield prefixApp.ArgumentExpression
            else currentExpr <- null
    }

let rec getIndexerExprOrIgnoreParens (expr: IFSharpExpression) =
    let appExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr)
    if isNotNull appExpr && appExpr.IsIndexerLike then
        getIndexerExprOrIgnoreParens appExpr else

    let indexerExpr = IndexerExprNavigator.GetByQualifierIgnoreIndexers(expr)
    if isNotNull indexerExpr then
        getIndexerExprOrIgnoreParens indexerExpr else

    expr.IgnoreParentParens()
