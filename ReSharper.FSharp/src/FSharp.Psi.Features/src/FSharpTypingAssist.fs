module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.TypingAssist

open System
open System.Collections.Generic
open JetBrains.Application.UI.ActionSystem.Text
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.TypingAssist
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CachingLexers
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.TextControl
open JetBrains.Util

[<SolutionComponent>]
type FSharpTypingAssist
        (lifetime, solution, settingsStore, cachingLexerService, commandProcessor, psiServices,
         externalIntellisenseHost, manager: ITypingAssistManager) as this =
    inherit TypingAssistLanguageBase<FSharpLanguage>
        (solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost)

    let indentingTokens =
        [| FSharpTokenType.EQUALS
           FSharpTokenType.LARROW
           FSharpTokenType.LPAREN
           FSharpTokenType.LBRACK
           FSharpTokenType.LBRACK_BAR
           FSharpTokenType.LBRACK_LESS
           FSharpTokenType.LBRACE
           FSharpTokenType.LQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED
           FSharpTokenType.BEGIN
           FSharpTokenType.THEN
           FSharpTokenType.STRUCT
           FSharpTokenType.CLASS
           FSharpTokenType.INTERFACE
           FSharpTokenType.TRY
           FSharpTokenType.WHEN |]
        |> HashSet

    let allowingNoIndentTokens =
        [| FSharpTokenType.RARROW
           FSharpTokenType.THEN
           FSharpTokenType.ELSE
           FSharpTokenType.DO |]
        |> HashSet

    let deindentingTokens =
        [| FSharpTokenType.RPAREN
           FSharpTokenType.RBRACK
           FSharpTokenType.BAR_RBRACK
           FSharpTokenType.GREATER_RBRACK
           FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.RQUOTE_UNTYPED
           FSharpTokenType.RBRACE
           FSharpTokenType.END |]
        |> HashSet

    let emptyBracketsToAddSpace =
        [| FSharpTokenType.LBRACE, FSharpTokenType.RBRACE
           FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
           FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK
           FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED |]
        |> HashSet

    let rightBracketsToAddSpace =
        emptyBracketsToAddSpace |> Seq.map snd |> HashSet

    let emptyQuotationsStrings =
        [| "<@@>", "<@"
           "<@@@@>", "<@@" |]
        |> dict
    
    let bracketsToAddIndent =
        [| FSharpTokenType.LPAREN, FSharpTokenType.RPAREN
           FSharpTokenType.LBRACE, FSharpTokenType.RBRACE
           FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
           FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK
           FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK
           FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED |]
        |> HashSet

    let leftBracketsToAddIndent =
        bracketsToAddIndent |> Seq.map fst |> HashSet

    let bracketsAllowingDeindent =
        [| FSharpTokenType.LBRACE
           FSharpTokenType.LBRACK
           FSharpTokenType.LBRACK_BAR |]
        |> HashSet
    
    let getCachingLexer textControl (lexer: outref<_>) =
        match cachingLexerService.GetCachingLexer(textControl) with
        | null -> false
        | cachingLexer ->
            lexer <- cachingLexer
            true

    let getIndentSize (textControl: ITextControl) =
        let document = textControl.Document
        let sourceFile = document.GetPsiSourceFile(this.Solution).NotNull("psiSourceFile is null for {0}", document)
        sourceFile.GetFormatterSettings(sourceFile.PrimaryPsiLanguage).INDENT_SIZE

    let getBaseIndentLine (document: IDocument) initialLine =
        let mutable line = initialLine
        while line > Line.O && document.GetLineText(line).IsWhitespace() do
            line <- line - Line.I

        if document.GetLineText(line).IsWhitespace() then initialLine else line

    let getAdditionalSpacesBeforeToken (textControl: ITextControl) offset lineStart =
        let mutable lexer = Unchecked.defaultof<_>

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then 0 else
        if not (rightBracketsToAddSpace.Contains(lexer.TokenType)) then 0 else

        let rightBracketOffset = lexer.TokenStart
        if not (findUnmatchedBracketToLeft lexer offset lineStart) then 0 else

        let leftBracketEndOffset = lexer.TokenEnd

        lexer.Advance()
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance() 

        // Emtpy list with spaces, add the same space as before caret.
        if lexer.TokenStart >= offset then offset - leftBracketEndOffset - 1 else

        // Empty list with no spaces, no additional spaces should be added.
        if lexer.TokenStart = rightBracketOffset then 0 else

        // Space before first list element.
        lexer.TokenStart - leftBracketEndOffset

    let trimTrailingSpacesAtOffset (textControl: ITextControl) (startOffset: outref<int>) trimAfterCaret =
        let isWhitespace c =
            c = ' ' || c = '\t'

        let document = textControl.Document
        let line = document.GetCoordsByOffset(startOffset).Line
        let lineStart = document.GetLineStartOffset(line)
        if document.GetText(TextRange(lineStart, startOffset)).IsWhitespace() then () else

        let mutable endOffset = startOffset
        let buffer = document.Buffer
        while startOffset > 0 && isWhitespace buffer.[startOffset - 1] do
            startOffset <- startOffset - 1

        let lineEndOffset = document.GetLineEndOffsetNoLineBreak(line)
        if trimAfterCaret = TrimTrailingSpaces.Yes then
            while endOffset < lineEndOffset && isWhitespace buffer.[endOffset] do
                endOffset <- endOffset + 1

        let additionalSpaces =
            if endOffset >= lineEndOffset then 0 else
            getAdditionalSpacesBeforeToken textControl endOffset lineStart
        
        if additionalSpaces > 0 then
            let replaceRange = TextRange(startOffset, endOffset)
            document.ReplaceText(replaceRange, String(' ', additionalSpaces))

        elif startOffset <> endOffset then
            document.DeleteText(TextRange(startOffset, endOffset))

    let trimTrailingSpaces (textControl: ITextControl) trimAfterCaret =
        let mutable offset = textControl.Caret.Offset()
        trimTrailingSpacesAtOffset textControl &offset trimAfterCaret
        offset

    let insertText (textControl: ITextControl) insertOffset text commandName =
        let inserted =
            this.PsiServices.Transactions.DocumentTransactionManager.DoTransaction(commandName, fun _ ->
                textControl.Document.InsertText(insertOffset, text)
                true)
        if inserted then
            textControl.Caret.MoveTo(insertOffset + text.Length, CaretVisualPlacement.DontScrollIfVisible)
        inserted

    let getLineWhitespaceIndent (textControl: ITextControl) line =
        let document = textControl.Document
        let buffer = document.Buffer
        let startOffset = document.GetLineStartOffset(line)
        let endOffset = document.GetLineEndOffsetNoLineBreak(line)

        let mutable pos = startOffset
        while pos < endOffset && Char.IsWhiteSpace(buffer.[pos]) do
            pos <- pos + 1

        pos - startOffset

    let getContinuedIndentLine (textControl: ITextControl) caretOffset continueByLeadingLParen =
        let document = textControl.Document
        let line = document.GetCoordsByOffset(caretOffset).Line
        if caretOffset = document.GetLineStartOffset(line) then line else

        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(caretOffset)) then line else

        let matcher = FSharpBracketMatcher()

        let rec tryFindContinuedLine line lineStartOffset hasLeadingLeftBracket =
            lexer.Advance(-1)
            if isNull lexer.TokenType then line else  

            if lexer.TokenStart <= lineStartOffset && not hasLeadingLeftBracket then line else

            let continuedLine =
                if deindentingTokens.Contains(lexer.TokenType) then
                    if not (matcher.FindMatchingBracket(lexer)) then line else
                    document.GetCoordsByOffset(lexer.TokenStart).Line
                else
                    if lexer.TokenStart >= lineStartOffset then line else line.Previous

            let lineStartOffset =
                if line = continuedLine then lineStartOffset else
                document.GetLineStartOffset(continuedLine)

            let hasLeadingLeftParen =
                continueByLeadingLParen = LeadingParenContinuesLine.Yes &&
                (lexer.TokenStart > lineStartOffset && lexer.TokenType == FSharpTokenType.LPAREN ||
                 hasLeadingLeftBracket && isIgnored lexer.TokenType)

            tryFindContinuedLine continuedLine lineStartOffset hasLeadingLeftParen

        let lineStartOffset = document.GetLineStartOffset(line)
        
        tryFindContinuedLine line lineStartOffset (lexer.TokenType == FSharpTokenType.LPAREN)

    let insertNewLineAt textControl insertPos indent trimAfterCaret =
        let insertPos = trimTrailingSpaces textControl trimAfterCaret
        let text = this.GetNewLineText(textControl) + String(' ', indent)
        insertText textControl insertPos text "Indent on Enter"
    
    let insertIndentFromLine textControl insertPos line =
        let indentSize = getLineWhitespaceIndent textControl line
        insertNewLineAt textControl insertPos indentSize

    let doDumpIndent (textControl: ITextControl) =
        let document = textControl.Document
        let buffer = document.Buffer

        let caretOffset = textControl.Caret.Offset()
        let caretLine = document.GetCoordsByOffset(caretOffset).Line
        let line = getContinuedIndentLine textControl caretOffset LeadingParenContinuesLine.No
        if line <> caretLine then insertIndentFromLine textControl caretOffset line else
        
        let startOffset = document.GetLineStartOffset(caretLine)
        let mutable pos = startOffset

        while pos < caretOffset && Char.IsWhiteSpace(buffer.[pos]) do
            pos <- pos + 1

        let indent = pos - startOffset
        insertNewLineAt textControl caretOffset indent

    let handleEnter (context: IActionContext) =
        let textControl = context.TextControl

        if this.HandlerEnterInTripleQuotedString(textControl) then true else
        if this.HandleEnterAddIndentAfterLeftBracket(textControl) then true else
        if this.HandleEnterFindLeftBracket(textControl) then true else
        if this.HandleEnterAddBiggerIndentFromBelow(textControl) then true else

        doDumpIndent textControl TrimTrailingSpaces.No

    let handleSpace (context: ITypingContext) =
        this.HandleSpaceInsideEmptyBrackets(context.TextControl)

    let handleQuote (context: ITypingContext) =
        this.HandlerThirdQuote(context.TextControl)

    let isActionHandlerAvailable = Predicate<_>(this.IsActionHandlerAvailabile2)
    let isTypingHandlerAvailable = Predicate<_>(this.IsTypingHandlerAvailable2)

    do
        manager.AddActionHandler(lifetime, TextControlActions.ENTER_ACTION_ID, this, Func<_,_>(handleEnter), isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, TextControlActions.BACKSPACE_ACTION_ID, this, Func<_,_>(this.HandleBackspacePressed), isActionHandlerAvailable)
        manager.AddTypingHandler(lifetime, ' ', this, Func<_,_>(handleSpace), isTypingHandlerAvailable)
        manager.AddTypingHandler(lifetime, '"', this, Func<_,_>(handleQuote), isTypingHandlerAvailable)

    member x.HandleEnterAddBiggerIndentFromBelow(textControl) =
        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let caretCoords = document.GetCoordsByOffset(caretOffset)
        let caretLine = caretCoords.Line

        if caretLine.Next >= document.GetLineCount() then false else
        if not (document.GetLineText(caretLine).IsWhitespace()) then false else

        match tryGetNestedIndentBelow cachingLexerService textControl caretLine (int caretCoords.Column) with
        | None -> false
        | Some (_, (Source indent | Comments indent)) ->
            insertNewLineAt textControl caretOffset indent TrimTrailingSpaces.No

    member x.HandleEnterFindLeftBracket(textControl) =
        let mutable lexer = Unchecked.defaultof<_>

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset
                offset > 0 && getCachingLexer textControl &lexer && lexer.FindTokenAt(offset - 1))

        if not isAvailable then false else

        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let lineStartOffset = document.GetLineStartOffset(document.GetCoordsByOffset(caretOffset).Line)

        if not (findUnmatchedBracketToLeft lexer caretOffset lineStartOffset) then false else

        lexer.Advance()
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        let indent = lexer.TokenStart - lineStartOffset
        insertNewLineAt textControl caretOffset indent TrimTrailingSpaces.Yes

    member x.HandleEnterAddIndentAfterLeftBracket(textControl) =
        let mutable lexer = Unchecked.defaultof<_>
        let mutable encounteredNewLine = false

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset
                if offset <= 0 then false else

                if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset - 1)) then false else

                while isIgnoredOrNewLine lexer.TokenType do
                    if lexer.TokenType == FSharpTokenType.NEW_LINE then
                        encounteredNewLine <- true
                    lexer.Advance(-1)

                not encounteredNewLine && allowingNoIndentTokens.Contains(lexer.TokenType) ||
                indentingTokens.Contains(lexer.TokenType))

        if not isAvailable then false else

        let tokenStart = lexer.TokenStart
        let tokenEnd = lexer.TokenEnd
        let tokenType = lexer.TokenType

        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let line = document.GetCoordsByOffset(tokenStart).Line

        if x.HandleEnterInsideSingleLineBrackets(textControl, lexer, line) then true else

        match tryGetNestedIndentBelowLine cachingLexerService textControl line with
        | Some (nestedIndentLine, (Source indent | Comments indent)) ->
            let caretLine = document.GetCoordsByOffset(caretOffset).Line
            if nestedIndentLine = caretLine then false else

            insertNewLineAt textControl caretOffset indent TrimTrailingSpaces.Yes
        | _ ->

        let indentSize =
            let defaultIndent = getIndentSize textControl
            if not (allowingNoIndentTokens.Contains(tokenType) || tokenType == FSharpTokenType.EQUALS) then
                let lineStart = document.GetLineStartOffset(line)
                tokenEnd - lineStart + defaultIndent else

            let prevIndentSize =
                let line = getContinuedIndentLine textControl tokenStart LeadingParenContinuesLine.Yes
                getLineWhitespaceIndent textControl line
            prevIndentSize + defaultIndent

        insertNewLineAt textControl caretOffset indentSize TrimTrailingSpaces.Yes

    member x.HandleEnterInsideSingleLineBrackets(textControl: ITextControl, lexer: CachingLexer, line) =
        let tokenType = lexer.TokenType
        let leftBracketStartOffset = lexer.TokenStart
        let leftBracketEndOffset = lexer.TokenEnd

        if not (leftBracketsToAddIndent.Contains(tokenType)) then false else
        if not (FSharpBracketMatcher().FindMatchingBracket(lexer)) then false else

        let document = textControl.Document
        let rightBracketStartOffset = lexer.TokenStart
        if document.GetCoordsByOffset(rightBracketStartOffset).Line <> line then false else

        lexer.Advance(-1)
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance(-1)

        let lastElementEndOffset = lexer.TokenEnd

        let shouldDeindent =
            lexer.FindTokenAt(leftBracketStartOffset - 1) |> ignore
            while isIgnored lexer.TokenType do
                lexer.Advance(-1)

            bracketsAllowingDeindent.Contains(tokenType) && lexer.TokenType != FSharpTokenType.NEW_LINE

        let baseIndentLength =
            if not shouldDeindent then
                let lineOffset = document.GetLineStartOffset(line)
                leftBracketStartOffset - lineOffset
            else
                let line = getContinuedIndentLine textControl leftBracketStartOffset LeadingParenContinuesLine.Yes
                getLineWhitespaceIndent textControl line

        let baseIndentString = x.GetNewLineText(textControl) + String(' ', baseIndentLength)
        let indentString = baseIndentString + String(' ',  getIndentSize textControl)

        if lastElementEndOffset = leftBracketEndOffset then
            let newText = indentString + baseIndentString
            document.ReplaceText(TextRange(lastElementEndOffset, rightBracketStartOffset), newText)
        else
            let firstElementStartOffset =
                lexer.FindTokenAt(leftBracketEndOffset) |> ignore
                while isIgnored lexer.TokenType do
                    lexer.Advance()
                lexer.TokenStart

            document.ReplaceText(TextRange(lastElementEndOffset, rightBracketStartOffset), baseIndentString)
            document.ReplaceText(TextRange(leftBracketEndOffset, firstElementStartOffset), indentString)

        textControl.Caret.MoveTo(leftBracketEndOffset + indentString.Length, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandlerEnterInTripleQuotedString(textControl: ITextControl) =
        let offset = textControl.Caret.Offset()
        let mutable lexer = Unchecked.defaultof<_>

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then false else

         // """{caret} foo"""
        if lexer.TokenType != FSharpTokenType.TRIPLE_QUOTED_STRING then false else
        if offset < lexer.TokenStart + 3 || offset > lexer.TokenEnd - 3 then false else

        let document = textControl.Document
        let strStartLine = document.GetCoordsByOffset(lexer.TokenStart).Line
        let strEndLine = document.GetCoordsByOffset(lexer.TokenEnd).Line
        if strStartLine <> strEndLine then false else

        let newLineString = this.GetNewLineText(textControl)
        document.InsertText(lexer.TokenEnd - 3, newLineString)
        document.InsertText(offset, newLineString)
        textControl.Caret.MoveTo(offset + newLineString.Length, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleBackspacePressed(context: IActionContext) =
        let textControl = context.TextControl
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        this.HandlerBackspaceInTripleQuotedString(textControl)

    member x.HandlerBackspaceInTripleQuotedString(textControl: ITextControl) =
        let offset = textControl.Caret.Offset()
        let mutable lexer = Unchecked.defaultof<_>

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then false else
        if lexer.TokenType != FSharpTokenType.TRIPLE_QUOTED_STRING || lexer.TokenStart = offset then false else

        let strStart = "\"\"\""
        let newLineLength = this.GetNewLineText(textControl).Length

        // """{caret}"""
        if lexer.TokenStart = offset - strStart.Length && lexer.TokenEnd = offset + strStart.Length then 
            textControl.Document.DeleteText(TextRange(offset - 1, offset + 3))
            textControl.Caret.MoveTo(offset - 1, CaretVisualPlacement.DontScrollIfVisible)
            true

        // """\n{caret} text here \n"""
        elif lexer.TokenStart + strStart.Length + newLineLength = offset then
            let document = textControl.Document
            let strStartCoords = document.GetCoordsByOffset(lexer.TokenStart)
            let strEndCoords = document.GetCoordsByOffset(lexer.TokenEnd)

            let caretLine = document.GetCoordsByOffset(offset).Line

            if caretLine <> strStartCoords.Line.Next then false else 
            if offset <> document.GetLineStartOffset(caretLine) then false else
            if strStartCoords.Line + (docLine 2) <> strEndCoords.Line then false else
            if strEndCoords.Column <> (docColumn 3) then false else

            let lastNewLineOffset = lexer.TokenEnd - strStart.Length - newLineLength
            textControl.Document.DeleteText(TextRange(lastNewLineOffset, lastNewLineOffset + newLineLength))
            textControl.Document.DeleteText(TextRange(offset - newLineLength, offset))
            textControl.Caret.MoveTo(offset - newLineLength, CaretVisualPlacement.DontScrollIfVisible)
            true

        else false
    
    member x.HandlerThirdQuote(textControl: ITextControl) =
        if textControl.Selection.HasSelection() then false else

        let offset = textControl.Caret.Offset()
        let mutable lexer = Unchecked.defaultof<_>

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset - 1)) then false else
        if lexer.TokenType != FSharpTokenType.STRING || lexer.GetTokenLength() <> 2 then false else
        if lexer.TokenEnd <> offset then false else

        textControl.Document.InsertText(offset, "\"\"\"\"")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleSpaceInsideEmptyBrackets(textControl: ITextControl) =
        let mutable lexer = Unchecked.defaultof<_>

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset
                if not (offset > 0 && getCachingLexer textControl &lexer) then false else

                if not (lexer.FindTokenAt(offset - 1)) then false else
                let left = lexer.TokenType
                let leftStart = lexer.TokenStart

                if not (lexer.FindTokenAt(offset)) then false else
                let right = lexer.TokenType

                emptyBracketsToAddSpace.Contains((left, right)) ||

                lexer.TokenType == FSharpTokenType.SYMBOLIC_OP &&
                leftStart = lexer.TokenStart &&

                let tokenText = lexer.GetCurrTokenText()
                let mutable leftBracketText = Unchecked.defaultof<_>
                emptyQuotationsStrings.TryGetValue(tokenText, &leftBracketText) &&
                tokenText.Substring(0, leftBracketText.Length) = leftBracketText)

        if not isAvailable then false else

        let offset = textControl.Caret.Offset()
        textControl.Document.InsertText(offset, "  ")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.GetNewLineText(textControl: ITextControl) =
        x.GetNewLineText(textControl.Document.GetPsiSourceFile(x.Solution))

    member x.IsActionHandlerAvailabile2(context) = base.IsActionHandlerAvailabile(context)
    member x.IsTypingHandlerAvailable2(context) = base.IsTypingHandlerAvailable(context)

    override x.IsSupported(textControl: ITextControl) =
        match textControl.Document.GetPsiSourceFile(x.Solution) with
        | null -> false
        | sourceFile ->

        sourceFile.IsValid() &&
        sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() &&
        sourceFile.Properties.ProvidesCodeModel

    interface ITypingHandler with
        member x.QuickCheckAvailability(textControl, sourceFile) =
            sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>()


type LineIndent =
    // Code indent, as seen by compiler.
    | Source of int

    // Fallback indent when no code is present on line. Used to guess the desired indentation.
    | Comments of int

let getLineIndent (cachingLexerService: CachingLexerService) (textControl: ITextControl) (line: Line) =
    let document = textControl.Document
    if line >= document.GetLineCount() then None else

    let startOffset = document.GetLineStartOffset(line)
    let endOffset = document.GetLineEndOffsetNoLineBreak(line)

    match cachingLexerService.GetCachingLexer(textControl) with
    | null -> None
    | lexer ->

    if not (lexer.FindTokenAt(startOffset)) then None else

    let mutable commentOffset = None
    while lexer.TokenType != null && lexer.TokenStart < endOffset && isIgnored lexer.TokenType do
        if commentOffset.IsNone && lexer.TokenType.IsComment then
            commentOffset <- Some (Comments (lexer.TokenStart - startOffset))
        lexer.Advance()

    let tokenType = lexer.TokenType
    if isNull tokenType || isIgnoredOrNewLine tokenType then commentOffset else
    Some (Source (lexer.TokenStart - startOffset))

let tryGetNestedIndentBelowLine cachingLexerService textControl line =
    match getLineIndent cachingLexerService textControl line with
    | None | Some (Comments _) -> None
    | Some (Source currentIndent) ->
        tryGetNestedIndentBelow cachingLexerService textControl line currentIndent

let tryGetNestedIndentBelow cachingLexerService textControl line currentIndent =
    let linesCount = textControl.Document.GetLineCount()

    let rec tryFindIndent (firstFoundCommentIndent: (Line * LineIndent) option) line =
        if line >= linesCount then firstFoundCommentIndent else

        let indent =
            getLineIndent cachingLexerService textControl line
            |> Option.map (fun indent -> line, indent)

        match indent, firstFoundCommentIndent with
        | Some (_, Source n), _ ->
            if n > currentIndent then indent else firstFoundCommentIndent

        | Some (_, Comments n), None when n > currentIndent -> tryFindIndent indent line.Next
        | _ -> tryFindIndent firstFoundCommentIndent line.Next

    tryFindIndent None line.Next

let findUnmatchedBracketToLeft (lexer: CachingLexer) offset minOffset =
    if lexer.TokenEnd > offset then
        lexer.Advance(-1)

    let matcher = FSharpBracketMatcher()
    let mutable foundToken = false

    while not foundToken && lexer.TokenStart >= minOffset do
        if FSharpTokenType.RightBraces.[lexer.TokenType] then
            if matcher.FindMatchingBracket(lexer) then
                lexer.Advance(-1)

        if FSharpTokenType.LeftBraces.[lexer.TokenType] then
            foundToken <- true
        else
            lexer.Advance(-1)

    foundToken

let isIgnored (tokenType: TokenNodeType) =
    isNotNull tokenType && (tokenType == FSharpTokenType.WHITESPACE || tokenType.IsComment)

let isIgnoredOrNewLine tokenType =
    isIgnored tokenType || tokenType == FSharpTokenType.NEW_LINE

let matchingBrackets =
    [| Pair.Of(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN)
       Pair.Of(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK)
       Pair.Of(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE) 
       Pair.Of(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK) 
       Pair.Of(FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK)
       Pair.Of(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED)
       Pair.Of(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED)
       Pair.Of(FSharpTokenType.BEGIN, FSharpTokenType.END)
       Pair.Of(FSharpTokenType.CLASS, FSharpTokenType.END)
       Pair.Of(FSharpTokenType.STRUCT, FSharpTokenType.END)
       Pair.Of(FSharpTokenType.INTERFACE, FSharpTokenType.END)
       Pair.Of(FSharpTokenType.WITH, FSharpTokenType.END)
       Pair.Of(FSharpTokenType.DO, FSharpTokenType.DONE) |]

type FSharpBracketMatcher() =
    inherit BracketMatcher(matchingBrackets)

[<RequireQualifiedAccess; Struct>]
type TrimTrailingSpaces =
    | Yes
    | No

[<RequireQualifiedAccess; Struct>]
type LeadingParenContinuesLine =
    | Yes
    | No
