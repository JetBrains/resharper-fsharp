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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CachingLexers
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.TextControl
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices.AstTraversal

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
           FSharpTokenType.IF
           FSharpTokenType.ELIF
           FSharpTokenType.THEN
           FSharpTokenType.STRUCT
           FSharpTokenType.CLASS
           FSharpTokenType.INTERFACE
           FSharpTokenType.TRY
           FSharpTokenType.WHEN
           FSharpTokenType.DO_BANG
           FSharpTokenType.YIELD
           FSharpTokenType.YIELD_BANG |]
        |> HashSet

    let allowingNoIndentTokens =
        [| FSharpTokenType.RARROW
           FSharpTokenType.THEN
           FSharpTokenType.ELSE
           FSharpTokenType.DO
           FSharpTokenType.WHEN
           FSharpTokenType.IF
           FSharpTokenType.ELIF |]
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

    let getOffsetInLine (document: IDocument) line offset =
        offset - document.GetLineStartOffset(line)

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
        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(caretOffset - 1)) then line else

        let matcher = FSharpBracketMatcher()

        let rec tryFindContinuedLine line lineStartOffset hasLeadingLeftBracket =
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

            lexer.Advance(-1)
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

    let insertCharInBrackets (textControl: ITextControl) (lexer: CachingLexer) (lChar, rChar) (lBracket, rBracket) leftBracketOnly =
        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        if lexer.FindTokenAt(offset - 1) && lexer.TokenType == lBracket && offset > lexer.TokenStart ||
           lexer.FindTokenAt(offset)     && lexer.TokenType == rBracket && offset < lexer.TokenEnd then

            let matcher = GenericBracketMatcher([| Pair.Of(lBracket, rBracket) |])
            let isAtLeftBracket = matcher.Direction(lexer.TokenType) = 1
            if not isAtLeftBracket && leftBracketOnly = LeftBracketOnly.Yes then false else

            if not (matcher.FindMatchingBracket(lexer)) then false else

            let document = textControl.Document
            let lBracketOffset, rBracketOffset =
                if isAtLeftBracket then offset, lexer.TokenEnd - 1 else lexer.TokenStart + 1, offset

            document.InsertText(rBracketOffset, rChar)
            document.InsertText(lBracketOffset, lChar)

            let lBracketLine = int (document.GetCoordsByOffset(lBracketOffset).Line)
            let rBracketLine = int (document.GetCoordsByOffset(rBracketOffset).Line)

            let shouldAddIndent =
                lexer.FindTokenAt(lBracketOffset - 1) &&
                lBracketLine <> rBracketLine && not (isLastTokenOnLine lexer)

            let lastLine =
                if not (lexer.FindTokenAt(rBracketOffset)) then rBracketLine else
                if isFirstTokenOnLine lexer then rBracketLine - 1 else rBracketLine 

            if shouldAddIndent then
                for line in lBracketLine + 1 .. lastLine do
                    document.InsertText(document.GetLineStartOffset(docLine line), " ")

            let newCaretOffset =
                if isAtLeftBracket then offset + 1 else
                if shouldAddIndent then offset + 2 + (lastLine - lBracketLine) else
                offset + 2

            textControl.Caret.MoveTo(newCaretOffset, CaretVisualPlacement.DontScrollIfVisible)
            true

        else false

    let handleEnter (context: IActionContext) =
        let textControl = context.TextControl

        if this.HandlerEnterInTripleQuotedString(textControl) then true else
        if this.HandleEnterAddIndent(textControl) then true else
        if this.HandleEnterInApp(textControl) then true else
        if this.HandleEnterBeforeDot(textControl) then true else
        if this.HandleEnterFindLeftBracket(textControl) then true else
        if this.HandleEnterAddBiggerIndentFromBelow(textControl) then true else

        let trimSpacesAfterCaret =
            let mutable lexer = Unchecked.defaultof<_>
            let offset = textControl.Caret.Offset()
            if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then TrimTrailingSpaces.No else

            while lexer.TokenType == FSharpTokenType.WHITESPACE do
                lexer.Advance()

            shouldTrimSpacesBeforeToken lexer.TokenType 

        doDumpIndent textControl trimSpacesAfterCaret

    let handleSpace (context: ITypingContext) =
        this.HandleSpaceInsideEmptyBrackets(context.TextControl)

    let isActionHandlerAvailable = Predicate<_>(this.IsActionHandlerAvailabile2)
    let isTypingHandlerAvailable = Predicate<_>(this.IsTypingHandlerAvailable2)

    do
        manager.AddActionHandler(lifetime, TextControlActions.ENTER_ACTION_ID, this, Func<_,_>(handleEnter), isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, TextControlActions.BACKSPACE_ACTION_ID, this, Func<_,_>(this.HandleBackspacePressed), isActionHandlerAvailable)
        manager.AddTypingHandler(lifetime, ' ', this, Func<_,_>(handleSpace), isTypingHandlerAvailable)

        manager.AddTypingHandler(lifetime, '\'', this, Func<_,_>(this.HandleQuoteTyped), isTypingHandlerAvailable)
        manager.AddTypingHandler(lifetime, '"', this, Func<_,_>(this.HandleQuoteTyped), isTypingHandlerAvailable)

        manager.AddTypingHandler(lifetime, '<', this, Func<_,_>(this.HandleLeftAngleBracket), isTypingHandlerAvailable)
        manager.AddTypingHandler(lifetime, '@', this, Func<_,_>(this.HandleAtTyped), isTypingHandlerAvailable)
        manager.AddTypingHandler(lifetime, '|', this, Func<_,_>(this.HandleBarTyped), isTypingHandlerAvailable)

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

    member x.HandleEnterAddIndent(textControl) =
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
        let tokenType = lexer.TokenType

        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let line = document.GetCoordsByOffset(tokenStart).Line

        if x.HandleEnterInsideSingleLineBrackets(textControl, lexer, line) then true else

        if leftBracketsToAddIndent.Contains(tokenType) && not (isSingleLineBrackets lexer document) &&
                not (isLastTokenOnLine lexer) && isFirstTokenOnLine lexer then false else

        match tryGetNestedIndentBelowLine cachingLexerService textControl line with
        | Some (nestedIndentLine, (Source indent | Comments indent)) ->
            let caretLine = document.GetCoordsByOffset(caretOffset).Line
            if nestedIndentLine = caretLine then false else

            insertNewLineAt textControl caretOffset indent TrimTrailingSpaces.Yes
        | _ ->

        let indentSize =
            let defaultIndent = getIndentSize textControl
            if not (allowingNoIndentTokens.Contains(tokenType) || tokenType == FSharpTokenType.EQUALS) then
                defaultIndent + getOffsetInLine document line tokenStart else

            let prevIndentSize =
                let line = getContinuedIndentLine textControl tokenStart LeadingParenContinuesLine.Yes
                getLineWhitespaceIndent textControl line
            prevIndentSize + defaultIndent

        insertNewLineAt textControl caretOffset indentSize TrimTrailingSpaces.Yes

    member x.HandleEnterInsideSingleLineBrackets(textControl: ITextControl, lexer: CachingLexer, line) =
        use cookie = LexerStateCookie.Create(lexer)

        let document = textControl.Document
        let tokenType = lexer.TokenType
        let leftBracketStartOffset = lexer.TokenStart
        let leftBracketEndOffset = lexer.TokenEnd
        let leftBracketLine = document.GetCoordsByOffset(leftBracketStartOffset).Line

        if not (findRightBracket lexer) then false else
        if document.GetCoordsByOffset(lexer.TokenStart).Line <> leftBracketLine then false else

        let rightBracketStartOffset = lexer.TokenStart

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
                getOffsetInLine document line leftBracketStartOffset
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

    member x.HandleEnterInApp(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer) then false else

        if not (lexer.FindTokenAt(offset - 1) &&
                let tokenType = lexer.TokenType
                (tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.RPAREN) ||

                lexer.FindTokenAt(offset) &&
                let tokenType = lexer.TokenType
                (tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.LPAREN)) then false else

        lexer.FindTokenAt(offset - 1) |> ignore
        if isFirstTokenOnLine lexer || isLastTokenOnLine lexer then false else 

        match x.CommitPsiOnlyAndProceedWithDirtyCaches(textControl, id).AsFSharpFile() with
        | null -> false
        | fsFile ->

        match fsFile.ParseTree with
        | None -> false
        | Some parseTree ->

        let document = textControl.Document
        let caretCoords = document.GetCoordsByOffset(offset)
        let caretLine = caretCoords.Line

        let mutable funExpr = None 
        let visitor =
            { new AstVisitorBase<_>() with
                member x.VisitExpr(path, _, defaultTraverse, expr) =
                    match expr with
                    | SynExpr.App (_, false, funcExpr, argExpr, m) when
                            funcExpr.Range.GetStartLine() = caretLine &&
                            offset >= document.GetOffset(funcExpr.Range.End) &&
                            offset <= document.GetOffset(argExpr.Range.Start) ->
                        funExpr <- Some expr
                        defaultTraverse expr

                    | _ -> defaultTraverse expr }

        Traverse(caretCoords.GetPos(), parseTree, visitor) |> ignore
        match funExpr with
        | None -> false
        | Some funExpr ->

        let indent =
            getOffsetInLine document caretLine (document.GetOffset(funExpr.Range.Start)) +
            getIndentSize textControl

        insertNewLineAt textControl offset indent TrimTrailingSpaces.Yes
    
    member x.HandleEnterBeforeDot(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer) then false else

        if not (lexer.FindTokenAt(offset)) then false else
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        if lexer.TokenType != FSharpTokenType.DOT then false else
        let dotOffset = lexer.TokenStart

        match x.CommitPsiOnlyAndProceedWithDirtyCaches(textControl, id).AsFSharpFile() with
        | null -> false
        | fsFile ->

        match fsFile.ParseTree with
        | None -> false
        | Some parseTree ->

        let document = textControl.Document
        let caretCoords = document.GetCoordsByOffset(offset)
        let caretLine = caretCoords.Line

        let visitor =
            { new AstVisitorBase<_>() with
                member x.VisitExpr(path, _, defaultTraverse, expr) =
                    if expr.Range.GetStartLine() <> caretLine then defaultTraverse expr else

                    match expr with
                    | SynExpr.DotGet (expr, rangeOfDot, _, _) when
                            dotOffset = document.GetOffset(rangeOfDot.Start) ->
                        Some expr
                    | SynExpr.LongIdent (_, LongIdentWithDots (lid, dots), _, _) when
                            dots |> List.exists (fun dot -> dotOffset = document.GetOffset(dot.Start)) ->
                        Some expr
                    | _ -> defaultTraverse expr }

        match Traverse(caretCoords.GetPos(), parseTree, visitor) with
        | None -> false
        | Some path ->

        let indent =
            getOffsetInLine document caretLine (document.GetOffset(path.Range.Start)) +
            getIndentSize textControl

        insertNewLineAt textControl dotOffset indent TrimTrailingSpaces.Yes
    
    member x.HandleEnterBeforeDotBak(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        let mutable lexer = Unchecked.defaultof<_>

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then false else
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        if lexer.TokenType != FSharpTokenType.DOT then false else

        let dotOffset = lexer.TokenStart
        if not (lexer.FindTokenAt(offset - 1)) then false else

        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance(-1)

        if isNull lexer.TokenType || lexer.TokenType == FSharpTokenType.NEW_LINE then false else

        let document = textControl.Document
        let indentString =
            use cookie = LexerStateCookie.Create(lexer)

            // (expr){caret}.bar 
            if FSharpTokenType.RightBraces.[lexer.TokenType] then
                let rightBracketPos = lexer.CurrentPosition
                if not (FSharpBracketMatcher().FindMatchingBracket(lexer)) then
                    lexer.CurrentPosition <- rightBracketPos
                else
                    let leftBracketType = lexer.TokenType
                    let leftBracketPos = lexer.CurrentPosition

                    lexer.Advance(-1)
                    while lexer.TokenType == FSharpTokenType.WHITESPACE do
                        lexer.Advance(-1)

                    let tokenType = lexer.TokenType
                    if isNull tokenType then
                        lexer.CurrentPosition <- leftBracketPos else

                    // ctor(expr){caret}.bar
                    if tokenType == FSharpTokenType.IDENTIFIER then () else

                    // list.[expr]{caret}.bar
                    if tokenType == FSharpTokenType.DOT && leftBracketType == FSharpTokenType.LBRACK then
                        lexer.Advance(-1)
                        while lexer.TokenType == FSharpTokenType.WHITESPACE do
                            lexer.Advance(-1)
                    if isNotNull lexer.TokenType then () else
                    lexer.CurrentPosition <- leftBracketPos

            
            let line = document.GetCoordsByOffset(lexer.TokenStart).Line
            let offsetInLine = getOffsetInLine document line lexer.TokenStart
            x.GetNewLineText(textControl) + String(' ', offsetInLine + getIndentSize textControl)

        textControl.Document.ReplaceText(TextRange(lexer.TokenEnd, dotOffset), indentString)
        textControl.Caret.MoveTo(offset + indentString.Length, CaretVisualPlacement.DontScrollIfVisible)
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

    member x.HandleQuoteTyped(context: ITypingContext) =
        let textControl = context.TextControl
        let typedChar = context.Char

        if context.EnsureWritable() <> EnsureWritableResult.SUCCESS then false else
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        if x.HandleThirdQuote(textControl, typedChar) then true else
        if x.SkipQuote(textControl, typedChar) then true else

        x.InsertPairQuote(textControl, typedChar)

    member x.SkipQuote(textControl: ITextControl, typedChar: char) =
        let buffer = textControl.Document.Buffer
        let offset = textControl.Caret.Offset()
        if offset >= buffer.Length || typedChar <> buffer.[offset] then false else

        let skipEndQuote (lexer: CachingLexer) =
            typedChar = getStringEndingQuote lexer.TokenType &&
            offset >= lexer.TokenEnd - getStringEndingQuotesOffset lexer.TokenType

        let skipEscapedQuoteInVerbatim (lexer: CachingLexer) =
            lexer.TokenType == FSharpTokenType.VERBATIM_STRING && typedChar = '\"'

        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset - 1)) then false else
        if not lexer.TokenType.IsStringLiteral || offset = lexer.TokenEnd then false else
        if not (skipEndQuote lexer || skipEscapedQuoteInVerbatim lexer) then false else

        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.InsertPairQuote(textControl: ITextControl, typedChar: char) =
        let mutable lexer = Unchecked.defaultof<_>
        let offset = textControl.Caret.Offset()

        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset)) then false else

        let tokenType = lexer.TokenType
        if tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then false else
        if tokenType.IsStringLiteral && typedChar <> getStringEndingQuote tokenType then false else

        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        // Do not prevent quoting code
        if not (isStringLiteralStopper lexer.TokenType) then false else

        textControl.Document.InsertText(offset, getCorresponingQuotesPair typedChar)
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)

        true 

    member x.HandleThirdQuote(textControl: ITextControl, typedChar: char) =
        if typedChar <> '\"' then false else

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

                emptyBracketsToAddSpace.Contains((left, right)) || isInsideEmptyQuoation lexer offset)

        if not isAvailable then false else

        let offset = textControl.Caret.Offset()
        textControl.Document.InsertText(offset, "  ")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleLeftAngleBracket(context: ITypingContext) =
        let textControl = context.TextControl
        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer) then false else

        insertCharInBrackets textControl lexer ("<", ">") listBrackets LeftBracketOnly.Yes

    member x.HandleBarTyped(context: ITypingContext) =
        let textControl = context.TextControl
        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer) then false else
        insertCharInBrackets textControl lexer ("|", "|") listBrackets LeftBracketOnly.No
    
    member x.HandleAtTyped(context: ITypingContext) =
        let textControl = context.TextControl
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let mutable lexer = Unchecked.defaultof<_>
        if not (getCachingLexer textControl &lexer && lexer.FindTokenAt(offset - 1)) then false else

        if x.MakeQuotation(textControl, lexer, offset) then true else
        if x.MakeEmptyQuotationUntyped(textControl, lexer, offset) then true else

        false
    
    member x.MakeQuotation(textControl: ITextControl, lexer: CachingLexer, offset) =
        if lexer.TokenType != FSharpTokenType.LESS then false else

        textControl.Document.InsertText(offset, "@@>")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.MakeEmptyQuotationUntyped(textControl: ITextControl, lexer: CachingLexer, offset) =
        if isInsideEmptyQuoation lexer offset && lexer.GetTokenLength() = 4 then
            textControl.Document.InsertText(offset, "@@")
            textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
            true else

        insertCharInBrackets textControl lexer ("@", "@") typedQuotationBrackes LeftBracketOnly.No
    
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
        elif not (FSharpTokenType.RightBraces.[lexer.TokenType]) then
            lexer.Advance(-1)

    foundToken

let isIgnored (tokenType: TokenNodeType) =
    isNotNull tokenType && (tokenType == FSharpTokenType.WHITESPACE || tokenType.IsComment)

let isIgnoredOrNewLine tokenType =
    isIgnored tokenType || tokenType == FSharpTokenType.NEW_LINE


let isLastTokenOnLine (lexer: CachingLexer) =
    use cookie = LexerStateCookie.Create(lexer)
    lexer.Advance()
    while isIgnored lexer.TokenType do
        lexer.Advance()
    isNull lexer.TokenType || lexer.TokenType == FSharpTokenType.NEW_LINE

let isFirstTokenOnLine (lexer: CachingLexer) =
    use cookie = LexerStateCookie.Create(lexer)

    lexer.Advance(-1)
    while isIgnored lexer.TokenType do
        lexer.Advance(-1)
    isNull lexer.TokenType || lexer.TokenType == FSharpTokenType.NEW_LINE


let bracketsToAddIndent =
    [| FSharpTokenType.LPAREN, FSharpTokenType.RPAREN
       FSharpTokenType.LBRACE, FSharpTokenType.RBRACE
       FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
       FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK
       FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK
       FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
       FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED |]
    |> HashSet

let leftBracketsToAddIndent: HashSet<TokenNodeType> =
    bracketsToAddIndent |> Seq.map fst |> HashSet

let findRightBracket (lexer: CachingLexer) =
    leftBracketsToAddIndent.Contains(lexer.TokenType) &&
    FSharpBracketMatcher().FindMatchingBracket(lexer)

let isSingleLineBrackets (lexer: CachingLexer) (document: IDocument) =
    use cookie = LexerStateCookie.Create(lexer)

    let startLine = document.GetCoordsByOffset(lexer.TokenStart).Line
    if not (findRightBracket lexer) then false else

    document.GetCoordsByOffset(lexer.TokenStart).Line = startLine


let shouldTrimSpacesBeforeToken (tokenType: TokenNodeType) =
    if isNull tokenType || FSharpTokenType.RightBraces.[tokenType] || tokenType.IsComment then TrimTrailingSpaces.No
    else TrimTrailingSpaces.Yes


let typedQuotationBrackes = FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
let listBrackets = FSharpTokenType.LBRACK, FSharpTokenType.RBRACK


let emptyQuotationsStrings =
    [| "<@@>", "<@"
       "<@@@@>", "<@@" |]
    |> dict

let isInsideEmptyQuoation (lexer: CachingLexer) offset =
    if not (lexer.FindTokenAt(offset - 1)) then false else
    let leftStart = lexer.TokenStart

    if not (lexer.FindTokenAt(offset)) then false else

    lexer.TokenType == FSharpTokenType.SYMBOLIC_OP &&
    leftStart = lexer.TokenStart &&

    let tokenText = lexer.GetCurrTokenText()
    let mutable leftBracketText = Unchecked.defaultof<_>
    emptyQuotationsStrings.TryGetValue(tokenText, &leftBracketText) &&
    tokenText.Substring(0, leftBracketText.Length) = leftBracketText

let stringLiteralStopperss =
    [| FSharpTokenType.WHITESPACE
       FSharpTokenType.NEW_LINE
       FSharpTokenType.LINE_COMMENT
       FSharpTokenType.BLOCK_COMMENT
       FSharpTokenType.SEMICOLON
       FSharpTokenType.COMMA
       FSharpTokenType.RPAREN
       FSharpTokenType.RBRACK
       FSharpTokenType.RBRACE
       FSharpTokenType.RQUOTE_TYPED
       FSharpTokenType.RQUOTE_UNTYPED
       FSharpTokenType.BAR_RBRACK
       FSharpTokenType.GREATER_RBRACK |]
    |> HashSet

let isStringLiteralStopper tokenType =
    stringLiteralStopperss.Contains(tokenType) ||
    isNotNull tokenType && tokenType.IsStringLiteral

let matchingBrackets =
    [| Pair.Of(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN)
       Pair.Of(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK)
       Pair.Of(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE)
       Pair.Of(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK)
       Pair.Of(FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK)
       Pair.Of(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED)
       Pair.Of(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED) |]

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

[<RequireQualifiedAccess; Struct>]
type LeftBracketOnly =
    | Yes
    | No
