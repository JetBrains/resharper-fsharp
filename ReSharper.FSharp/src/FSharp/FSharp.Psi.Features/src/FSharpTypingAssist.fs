module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.TypingAssist

open System
open System.Collections.Generic
open FSharp.Compiler.Syntax
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.Application.CommandProcessing
open JetBrains.Application.Parts
open JetBrains.Application.UI.ActionSystem.Text
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Options
open JetBrains.ReSharper.Feature.Services.TypingAssist
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CachingLexers
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.TextControl.DataContext
open JetBrains.Util

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FSharpTypingAssist(lifetime, dependencies) as this =
    inherit TypingAssistLanguageBase<FSharpLanguage>(dependencies)

    static let nodeTypeSet (tokenTypes: NodeType[]) =
        NodeTypeSet(tokenTypes)

    static let indentFromToken =
        [| FSharpTokenType.LBRACK_LESS
           FSharpTokenType.LQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED
           FSharpTokenType.STRUCT
           FSharpTokenType.CLASS
           FSharpTokenType.INTERFACE
           FSharpTokenType.TRY
           FSharpTokenType.NEW
           FSharpTokenType.LAZY |]
        |> HashSet

    static let indentFromPrevLine =
        [| FSharpTokenType.FUNCTION
           FSharpTokenType.EQUALS
           FSharpTokenType.LARROW
           FSharpTokenType.MATCH
           FSharpTokenType.WHILE
           FSharpTokenType.WHEN
           FSharpTokenType.DO
           FSharpTokenType.DO_BANG
           FSharpTokenType.YIELD
           FSharpTokenType.YIELD_BANG
           FSharpTokenType.BEGIN |]
        |> HashSet

    static let indentTokens =
        let hs = HashSet()
        hs.AddRange(indentFromToken)
        hs.AddRange(indentFromPrevLine)
        hs

    static let allowKeepIndent =
        [| FSharpTokenType.LPAREN
           FSharpTokenType.LBRACK
           FSharpTokenType.LBRACE
           FSharpTokenType.LBRACK_BAR
           FSharpTokenType.EQUALS
           FSharpTokenType.LARROW
           FSharpTokenType.RARROW
           FSharpTokenType.IF
           FSharpTokenType.THEN
           FSharpTokenType.ELIF
           FSharpTokenType.ELSE
           FSharpTokenType.MATCH
           FSharpTokenType.WHILE
           FSharpTokenType.WHEN
           FSharpTokenType.DO
           FSharpTokenType.DO_BANG
           FSharpTokenType.YIELD
           FSharpTokenType.YIELD_BANG
           FSharpTokenType.BEGIN |]
        |> HashSet

    static let deindentingTokens =
        [| FSharpTokenType.RPAREN
           FSharpTokenType.RBRACK
           FSharpTokenType.BAR_RBRACK
           FSharpTokenType.GREATER_RBRACK
           FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.RQUOTE_UNTYPED
           FSharpTokenType.RBRACE
           FSharpTokenType.END |]
        |> HashSet

    static let emptyBracketsToAddSpace =
        [| FSharpTokenType.LBRACE, FSharpTokenType.RBRACE
           FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
           FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK
           FSharpTokenType.LBRACE_BAR, FSharpTokenType.BAR_RBRACE
           FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED |]
        |> HashSet

    static let rightBracketsToAddSpace =
        emptyBracketsToAddSpace |> Seq.map snd |> HashSet

    static let bracketsAllowingDeindent =
        [| FSharpTokenType.LBRACE
           FSharpTokenType.LBRACK
           FSharpTokenType.LBRACK_BAR
           FSharpTokenType.LPAREN |]
        |> HashSet

    static let leftBrackets =
        [| FSharpTokenType.LPAREN
           FSharpTokenType.LBRACK
           FSharpTokenType.LBRACE |]
        |> HashSet

    static let tokensSuitableForRightBracket =
        [| FSharpTokenType.WHITESPACE
           FSharpTokenType.NEW_LINE
           FSharpTokenType.LINE_COMMENT
           FSharpTokenType.BLOCK_COMMENT
           FSharpTokenType.SEMICOLON
           FSharpTokenType.COMMA
           FSharpTokenType.RPAREN
           FSharpTokenType.RBRACK
           FSharpTokenType.RBRACE
           FSharpTokenType.GREATER_RBRACK
           FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.RQUOTE_UNTYPED |]
        |> HashSet

    static let rightBracketsText =
        [| '(', ")"
           '[', "]"
           '{', "}" |]
        |> dict

    static let bracketTypesForRightBracketChar =
        [| ')', (FSharpTokenType.LPAREN, FSharpTokenType.RPAREN)
           ']', (FSharpTokenType.LBRACK, FSharpTokenType.RBRACK)
           '}', (FSharpTokenType.LBRACE, FSharpTokenType.RBRACE)
           '>', (FSharpTokenType.LESS,   FSharpTokenType.GREATER) |]
        |> dict

    static let leftToRightBracket =
        [| '(', ')'
           '[', ']'
           '{', '}' |]
        |> dict

    static let rightToLeftBracket =
        leftToRightBracket
        |> Seq.map(fun (KeyValue (key, value)) -> value, key)
        |> dict

    static let bracketToTokenType =
        [| '(', FSharpTokenType.LPAREN
           ')', FSharpTokenType.RPAREN
           '[', FSharpTokenType.LBRACK
           ']', FSharpTokenType.RBRACK
           '{', FSharpTokenType.LBRACE
           '}', FSharpTokenType.RBRACE |]
        |> dict


    static let stringLiteralStoppers =
        let tokenTypes: NodeType[] =
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
        nodeTypeSet tokenTypes

    static let interpolatedStrings =
        let tokenTypes: NodeType[] =
            [| FSharpTokenType.REGULAR_INTERPOLATED_STRING
               FSharpTokenType.REGULAR_INTERPOLATED_STRING_START
               FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.REGULAR_INTERPOLATED_STRING_END
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END |]
        nodeTypeSet tokenTypes

    static let skipCloseBrackets =
        let tokenTypes: NodeType[] =
            [| FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.REGULAR_INTERPOLATED_STRING_END
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE
               FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END |]
        nodeTypeSet tokenTypes

    let isStringLiteralStopper (tokenType: TokenNodeType) =
        stringLiteralStoppers[tokenType] || isNotNull tokenType && tokenType.IsStringLiteral


    let strings =
        FSharpTokenType.Strings.Union(FSharpTokenType.InterpolatedStrings)


    static let infixOpTokens =
        [| FSharpTokenType.AMP_AMP
           FSharpTokenType.PLUS
           FSharpTokenType.MINUS |]
        |> HashSet

    let isInfixOp (lexer: ILexer) =
        if isNull lexer.TokenType then false else

        infixOpTokens.Contains(lexer.TokenType) ||

        lexer.TokenType == FSharpTokenType.SYMBOLIC_OP &&
        IsOperatorDisplayName (lexer.GetTokenText())


    static let charOffsetInRightBrackets: IDictionary<char, (TokenNodeType * int)[]> =
        [| '|', [| FSharpTokenType.BAR_RBRACK, 0
                   FSharpTokenType.BAR_RBRACE, 0
                   FSharpTokenType.GREATER_BAR_RBRACK, 1 |]

           '>', [| FSharpTokenType.GREATER_RBRACK, 0
                   FSharpTokenType.GREATER_BAR_RBRACK, 0
                   FSharpTokenType.RQUOTE_TYPED, 1
                   FSharpTokenType.RQUOTE_UNTYPED, 2 |]

           ']', [| FSharpTokenType.BAR_RBRACK, 1
                   FSharpTokenType.GREATER_RBRACK, 1
                   FSharpTokenType.GREATER_BAR_RBRACK, 2 |]

           '}', [| FSharpTokenType.BAR_RBRACE, 1
                   FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE, 0
                   FSharpTokenType.REGULAR_INTERPOLATED_STRING_END, 0
                   FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE, 0
                   FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END, 0
                   FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE, 0
                   FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END, 0 |]

           '@', [| FSharpTokenType.RQUOTE_TYPED, 0
                   FSharpTokenType.RQUOTE_UNTYPED, 0
                   FSharpTokenType.RQUOTE_UNTYPED, 1 |]
           ')', [| FSharpTokenType.LPAREN_STAR_RPAREN, 2 |] |]
        |> dict


    static let typedQuotationBrackets = FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
    static let listBrackets = FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
    static let recordBrackets = FSharpTokenType.LBRACE, FSharpTokenType.RBRACE


    static let tryDeindentTokens: IDictionary<TokenNodeType, TokenNodeType[]> =
        [| FSharpTokenType.THEN, [| FSharpTokenType.IF; FSharpTokenType.ELIF |]
           FSharpTokenType.DO,   [| FSharpTokenType.WHILE; FSharpTokenType.FOR |] |]
        |> dict


    static let bracketsToAddIndent =
        [| FSharpTokenType.LPAREN, FSharpTokenType.RPAREN
           FSharpTokenType.LBRACE, FSharpTokenType.RBRACE
           FSharpTokenType.LBRACK, FSharpTokenType.RBRACK
           FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK
           FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK
           FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED
           FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED |]
        |> HashSet

    static let leftBracketsToAddIndent: HashSet<TokenNodeType> =
        bracketsToAddIndent |> Seq.map fst |> HashSet

    let getNode (textControl: ITextControl) offset : 'T when 'T :> ITreeNode =
        if offset < 0 then null else

        let fsFile = textControl.GetFSharpFile(dependencies.Solution)
        fsFile.GetNode<'T>(DocumentOffset(textControl.Document, offset))

    let advanceToPrevToken (lexer: CachingLexer) =
        use cookie = LexerStateCookie.Create(lexer)
        lexer.Advance(-1)
        while isIgnored lexer.TokenType do
            lexer.Advance(-1)
        lexer.TokenEnd

    let findRightBracket (lexer: CachingLexer) =
        leftBracketsToAddIndent.Contains(lexer.TokenType) &&
        FSharpBracketMatcher().FindMatchingBracket(lexer)

    let isSingleLineBrackets (lexer: CachingLexer) (document: IDocument) =
        use cookie = LexerStateCookie.Create(lexer)

        let startLine = document.GetCoordsByOffset(lexer.TokenStart).Line
        if not (findRightBracket lexer) then false else

        document.GetCoordsByOffset(lexer.TokenStart).Line = startLine


    static let emptyQuotationsStrings =
        [| "<@@>", "<@"
           "<@@@@>", "<@@" |]
        |> dict

    let isInsideEmptyQuotation (lexer: CachingLexer) offset =
        if not (lexer.FindTokenAt(offset - 1)) then false else
        let leftStart = lexer.TokenStart

        if not (lexer.FindTokenAt(offset)) then false else

        lexer.TokenType == FSharpTokenType.SYMBOLIC_OP &&
        leftStart = lexer.TokenStart &&

        let tokenText = lexer.GetTokenText()
        let mutable leftBracketText = Unchecked.defaultof<_>
        emptyQuotationsStrings.TryGetValue(tokenText, &leftBracketText) &&
        tokenText.Substring(0, leftBracketText.Length) = leftBracketText


    let getIndentSize (textControl: ITextControl) =
        let document = textControl.Document
        let sourceFile = document.GetPsiSourceFile(this.Solution).NotNull("psiSourceFile is null for {0}", document)
        sourceFile.GetFormatterSettings(sourceFile.PrimaryPsiLanguage).INDENT_SIZE

    let getOffsetInLine (document: IDocument) line offset =
        offset - document.GetLineStartOffset(line)

    let getAdditionalSpacesBeforeToken (textControl: ITextControl) offset lineStart =
        let lexer = this.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(offset)) then 0 else

        // Always add a single space before -> so completion works nicely
        if lexer.TokenType == FSharpTokenType.RARROW then 1 else

        if not (rightBracketsToAddSpace.Contains(lexer.TokenType)) then 0 else

        let rightBracketOffset = lexer.TokenStart
        if not (findUnmatchedBracketToLeft lexer offset lineStart) then 0 else

        let leftBracketEndOffset = lexer.TokenEnd

        lexer.Advance()
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        // Empty list with spaces, add the same space as before caret.
        if lexer.TokenStart >= offset then offset - leftBracketEndOffset - 1 else

        // Empty list with no spaces, no additional spaces should be added.
        if lexer.TokenStart = rightBracketOffset then 0 else

        // Space before first list element.
        lexer.TokenStart - leftBracketEndOffset

    let trimTrailingSpaces (textControl: ITextControl) trimAfterCaret =
        let mutable offset = textControl.Caret.Offset()
        this.TrimTrailingSpacesAtOffset(textControl, &offset, trimAfterCaret)
        offset

    let insertText (textControl: ITextControl) insertOffset text commandName moveCaretDelta =
        let inserted =
            this.PsiServices.Transactions.DocumentTransactionManager.DoTransaction(commandName, fun _ ->
                textControl.Document.InsertText(insertOffset, text)
                true)
        if inserted then
            let newCaretPos = Option.defaultWith (fun _ -> insertOffset + text.Length) moveCaretDelta
            textControl.Caret.MoveTo(newCaretPos, CaretVisualPlacement.DontScrollIfVisible)
        inserted

    let getLineWhitespaceIndent (textControl: ITextControl) line =
        let document = textControl.Document
        let buffer = document.Buffer
        let startOffset = document.GetLineStartOffset(line)
        let endOffset = document.GetLineEndOffsetNoLineBreak(line)

        let mutable pos = startOffset
        while pos < endOffset && Char.IsWhiteSpace(buffer[pos]) do
            pos <- pos + 1

        pos - startOffset

    let getContinuedIndentLine (textControl: ITextControl) caretOffset continueByLeadingLParen =
        let document = textControl.Document
        let line = document.GetCoordsByOffset(caretOffset).Line
        if caretOffset = document.GetLineStartOffset(line) then line else

        let lexer = this.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(caretOffset - 1)) then line else

        let rec tryFindContinuedLine line lineStartOffset hasLeadingLeftBracket =
            if isNull lexer.TokenType then line else

            if lexer.TokenEnd <= lineStartOffset && not hasLeadingLeftBracket then line else

            let interpolatedStringExpr =
                if not (isInterpolatedStringEndToken lexer.TokenType) then null else

                let fsFile = textControl.GetFSharpFile(dependencies.Solution)
                fsFile.GetNode<IInterpolatedStringExpr>(DocumentOffset(document, lexer.TokenEnd - 1))

            if isNotNull interpolatedStringExpr then
                let interpolatedStringExprStartOffset = interpolatedStringExpr.GetTreeStartOffset().Offset
                while lexer.TokenStart > interpolatedStringExprStartOffset do
                    lexer.Advance(-1)

            let continuedLine =
                if deindentingTokens.Contains(lexer.TokenType) then
                    if not (FSharpBracketMatcher().FindMatchingBracket(lexer)) then line else
                    document.GetCoordsByOffset(lexer.TokenStart).Line
                else
                    if lexer.TokenStart >= lineStartOffset then line else

                    document.GetCoordsByOffset(lexer.TokenStart).Line

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

    let insertNewLineAt textControl indent trimAfterCaret =
        use command = this.CommandProcessor.UsingCommand("New Line")
        let insertPos = trimTrailingSpaces textControl trimAfterCaret
        let text = this.GetNewLineText(textControl) + String(' ', indent)
        insertText textControl insertPos text "Indent on Enter" None

    let insertIndentFromLine textControl line =
        let indentSize = getLineWhitespaceIndent textControl line
        insertNewLineAt textControl indentSize

    let isInsideString textControl =
        let lexer = this.GetCachingLexer(textControl)
        let offset = textControl.Caret.Offset()
        isNotNull lexer && lexer.FindTokenAt(offset - 1) &&

        FSharpTokenType.Strings[lexer.TokenType] &&

        offset > lexer.TokenStart && offset < lexer.TokenEnd

    let doDumpIndent (textControl: ITextControl) =
        let document = textControl.Document
        let buffer = document.Buffer

        if isInsideString textControl then
            insertNewLineAt textControl 0 else

        let caretOffset = textControl.Caret.Offset()
        let caretLine = document.GetCoordsByOffset(caretOffset).Line
        let line = getContinuedIndentLine textControl caretOffset LeadingParenContinuesLine.No
        if line <> caretLine then insertIndentFromLine textControl line else

        let startOffset = document.GetLineStartOffset(caretLine)
        let mutable pos = startOffset

        while pos < caretOffset && Char.IsWhiteSpace(buffer[pos]) do
            pos <- pos + 1

        let indent = pos - startOffset
        insertNewLineAt textControl indent

    let insertCharInBrackets (context: ITypingContext) (lexer: CachingLexer) chars brackets leftBracketOnly =
        let textControl = context.TextControl
        let typed = context.Char
        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let lChar, rChar = chars
        let lBracket, rBracket = brackets

        let canInsertLeft (lexer: ILexer) =
            lexer.TokenType == lBracket && typed = lChar && offset > lexer.TokenStart

        let canInsertRight (lexer: ILexer) =
            lexer.TokenType == rBracket && typed = rChar && offset < lexer.TokenEnd

        if not (tokenAtOffsetIs (offset - 1) canInsertLeft lexer ||
                tokenAtOffsetIs offset canInsertRight lexer) then false else

        let matcher = GenericBracketMatcher([| Pair.Of(lBracket, rBracket) |])
        let isAtLeftBracket = matcher.Direction(lexer.TokenType) = 1
        if not isAtLeftBracket && leftBracketOnly = LeftBracketOnly.Yes then false else

        if not (matcher.FindMatchingBracket(lexer)) then false else

        let document = textControl.Document
        let lBracketOffset, rBracketOffset =
            if isAtLeftBracket then offset, lexer.TokenEnd - 1 else lexer.TokenStart + 1, offset

        document.InsertText(rBracketOffset, string rChar)
        document.InsertText(lBracketOffset, string lChar)

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
                document.InsertText(document.GetLineStartDocumentOffset(docLine line), " ")

        let newCaretOffset =
            if isAtLeftBracket then offset + 1 else
            if shouldAddIndent then offset + 2 + (lastLine - lBracketLine) else
            offset + 2

        textControl.Caret.MoveTo(newCaretOffset, CaretVisualPlacement.DontScrollIfVisible)
        true

    let handleEnter (context: IActionContext) =
        let textControl = context.TextControl

        if this.HandlerEnterInTripleQuotedString(textControl) then true else
        if this.HandleEnterInLineComment(textControl) then true else
        if this.HandleEnterAfterSingleLineIf(textControl) then true else
        if this.HandleEnterAddIndent(textControl) then true else
        if this.HandleEnterAfterErrorAndInfixOp(textControl) then true else
        if this.HandleEnterInApp(textControl) then true else
        if this.HandleEnterBeforeDot(textControl) then true else
        if this.HandleEnterFindLeftBracket(textControl) then true else
        if this.HandleEnterAddBiggerIndentFromBelow(textControl) then true else

        let trimSpacesAfterCaret =
            let lexer = this.GetCachingLexer(textControl)
            if not (isNotNull lexer && lexer.FindTokenAt(textControl.Caret.Offset())) then TrimTrailingSpaces.No else

            while lexer.TokenType == FSharpTokenType.WHITESPACE do
                lexer.Advance()

            shouldTrimSpacesBeforeToken lexer.TokenType

        doDumpIndent textControl trimSpacesAfterCaret

    let handleStartNewLineBeforePressed (context: IActionContext) =
        use _ = this.CommandProcessor.UsingCommand("Start New Line Before")

        let textControl = context.TextControl
        textControl.Selection.Delete()

        let document = textControl.Document
        let caretCoords = textControl.Caret.Position.Value.ToDocLineColumn()
        let caretLine = caretCoords.Line
        let lineStartOffset = document.GetLineStartOffset(caretLine)

        let indent =
            match getLineIndent dependencies.CachingLexerService textControl caretLine with
            | Some lineIndent -> lineIndent.Indent
            | _ -> 0

        let text = String(' ', indent) + this.GetNewLineText(textControl)
        insertText textControl lineStartOffset text "Start New Line Before" (Some (lineStartOffset + indent))

    let handleStartNewLinePressed (context: IActionContext) =
        use _ = this.CommandProcessor.UsingCommand("Start New Line")

        let textControl = context.TextControl
        textControl.Selection.Delete()

        let document = textControl.Document
        let caretLine = textControl.Caret.Position.Value.ToDocLineColumn().Line
        let lineEndOffset = document.GetLineEndOffsetNoLineBreak(caretLine)
        textControl.Caret.MoveTo(lineEndOffset, CaretVisualPlacement.DontScrollIfVisible)

        handleEnter(context)

    let handleSpace (context: ITypingContext) =
        this.HandleSpaceInsideEmptyBrackets(context.TextControl)

    do
        let isActionHandlerAvailable = Predicate<_>(this.IsActionHandlerAvailable2)
        let isTypingHandlerAvailable = Predicate<_>(this.IsTypingHandlerAvailable2)
        let isSmartParensHandlerAvailable = Predicate<_>(this.IsTypingSmartParenthesisHandlerAvailable2)
        let manager = dependencies.TypingAssistManager

        manager.AddActionHandler(lifetime, TextControlActions.ActionIds.Enter, this, handleEnter, isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, EditorStartNewLineBeforeAction.ACTION_ID, this, handleStartNewLineBeforePressed, isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, EditorStartNewLineAction.ACTION_ID, this, handleStartNewLinePressed, isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, TextControlActions.ActionIds.Backspace, this, Func<_,_>(this.HandleBackspacePressed), isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, TextControlActions.ActionIds.Tab, this, Func<_,_>(this.HandleTabPressed), isActionHandlerAvailable)
        manager.AddActionHandler(lifetime, TextControlActions.ActionIds.TabLeft, this, Func<_,_>(this.HandleTabLeftPressed), isActionHandlerAvailable)
        manager.AddTypingHandler(lifetime, ' ', this, handleSpace, isTypingHandlerAvailable)

        manager.AddTypingHandler(lifetime, '\'', this, Func<_,_>(this.HandleSingleQuoteTyped), isTypingHandlerAvailable)
        manager.AddTypingHandler(lifetime, '"', this, Func<_,_>(this.HandleQuoteTyped), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '`', this, Func<_,_>(this.HandleBacktickTyped), isSmartParensHandlerAvailable)

        manager.AddTypingHandler(lifetime, '(', this, Func<_,_>(this.HandleLeftBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '[', this, Func<_,_>(this.HandleLeftBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '{', this, Func<_,_>(this.HandleLeftBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, ')', this, Func<_,_>(this.HandleRightBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, ']', this, Func<_,_>(this.HandleRightBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '}', this, Func<_,_>(this.HandleRightBracket), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '>', this, Func<_,_>(this.HandleRightBracket), isSmartParensHandlerAvailable)

        manager.AddTypingHandler(lifetime, '<', this, Func<_,_>(this.HandleRightAngleBracketTyped), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '@', this, Func<_,_>(this.HandleAtTyped), isSmartParensHandlerAvailable)
        manager.AddTypingHandler(lifetime, '|', this, Func<_,_>(this.HandleBarTyped), isSmartParensHandlerAvailable)

    member x.HandleEnterAddBiggerIndentFromBelow(textControl) =
        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let caretCoords = document.GetCoordsByOffset(caretOffset)
        let caretLine = caretCoords.Line

        if caretLine.Next >= document.GetLineCount() then false else
        let lineStartOffset = document.GetLineStartOffset(caretLine)

        let lexer = x.GetCachingLexer(textControl)
        let mutable seenComment = false
        if not (isNotNull lexer && lexer.FindTokenAt(lineStartOffset)) then false else

        while lexer.TokenType == FSharpTokenType.WHITESPACE ||
                isNotNull lexer.TokenType && lexer.TokenType.IsComment && lexer.TokenStart < caretOffset do
            seenComment <- seenComment || lexer.TokenType.IsComment
            lexer.Advance()

        if lexer.TokenType != FSharpTokenType.NEW_LINE then false else

        let currentIndent = if seenComment then 0 else int caretCoords.Column
        match tryGetNestedIndentBelow dependencies.CachingLexerService textControl caretLine seenComment currentIndent with
        | None -> false
        | Some (_, (Source indent | Comments indent)) ->
            insertNewLineAt textControl indent TrimTrailingSpaces.No

    member x.HandleEnterInLineComment(textControl) =
        x.DoHandleEnterInLineCommentPressed
            (textControl, "//", "///", fun tokenType -> tokenType == FSharpTokenType.LINE_COMMENT)

    member x.HandleEnterFindLeftBracket(textControl) =
        let lexer = x.GetCachingLexer(textControl)

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset.Offset
                offset > 0 && isNotNull lexer && lexer.FindTokenAt(offset - 1))

        if not isAvailable then false else

        let document = textControl.Document
        let caretOffset = textControl.Caret.Offset()
        let caretLine = document.GetCoordsByOffset(caretOffset).Line
        let lineStartOffset = document.GetLineStartOffset(caretLine)

        if not (findUnmatchedBracketToLeft lexer caretOffset lineStartOffset) then false else

        let leftBracketOffset = lexer.TokenStart
        let leftBracketType = lexer.TokenType

        lexer.Advance()
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        let indent =
            // { new IInterface with {caret} }
            if leftBracketType == FSharpTokenType.LBRACE && lexer.TokenType == FSharpTokenType.NEW then
                let braceOffset = getOffsetInLine document caretLine leftBracketOffset
                let defaultIndent = getIndentSize textControl
                braceOffset + defaultIndent

            else lexer.TokenStart - lineStartOffset

        insertNewLineAt textControl indent TrimTrailingSpaces.Yes

    member x.HandleEnterAddIndent(textControl) =
        let lexer = x.GetCachingLexer(textControl)
        let mutable encounteredNewLine = false

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset.Offset
                if not (offset > 0 && isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else

                while isIgnoredOrNewLine lexer.TokenType do
                    if lexer.TokenType == FSharpTokenType.NEW_LINE then
                        encounteredNewLine <- true
                    lexer.Advance(-1)

                not encounteredNewLine && allowKeepIndent.Contains(lexer.TokenType) ||
                indentTokens.Contains(lexer.TokenType))

        if not isAvailable then false else

        let tokenStart = lexer.TokenStart
        let tokenType = lexer.TokenType

        let document = textControl.Document
        let offset = textControl.Caret.Offset()
        let line = document.GetCoordsByOffset(tokenStart).Line

        if not encounteredNewLine && x.HandleEnterInEmptyLambda(textControl, lexer) then true else
        if x.HandleEnterInsideSingleLineBrackets(textControl, lexer, line) then true else

        if leftBracketsToAddIndent.Contains(tokenType) && not (isSingleLineBrackets lexer document) &&
                not (isLastTokenOnLine lexer) && isFirstTokenOnLine lexer then false else

        let caretLine = document.GetCoordsByOffset(offset).Line
        match tryGetNestedIndentBelowLine dependencies.CachingLexerService textControl line with
        | Some (nestedIndentLine, (Source indent | Comments indent)) ->
            if nestedIndentLine = caretLine then false else

            insertNewLineAt textControl indent TrimTrailingSpaces.Yes
        | _ ->

        if x.DeindentAfterUnfinishedExpr(textControl, lexer) then true else
        if x.IndentAfterRArrow(textControl, lexer) then true else

        if lexer.FindTokenAt(offset) && isFirstTokenOnLine lexer && not (isLastTokenOnLine lexer) then false else

        let indentSize =
            lexer.FindTokenAt(offset - 1) |> ignore
            
            let defaultIndent = getIndentSize textControl
            match getLineIndent dependencies.CachingLexerService textControl caretLine with
            | Some(Comments n) -> n
            | _ ->

            if tokenType == FSharpTokenType.EQUALS && not encounteredNewLine && isFirstTokenOnLine lexer then
                getOffsetInLine document line tokenStart else

            if indentFromToken.Contains(tokenType) then
                defaultIndent + getOffsetInLine document line tokenStart else

            let prevIndentSize =
                let line = getContinuedIndentLine textControl tokenStart LeadingParenContinuesLine.Yes
                getLineWhitespaceIndent textControl line
            prevIndentSize + defaultIndent

        insertNewLineAt textControl indentSize TrimTrailingSpaces.Yes

    member x.DeindentAfterUnfinishedExpr(textControl: ITextControl, lexer: CachingLexer) =
        let mutable allowedTokens = null
        if not (tryDeindentTokens.TryGetValue(lexer.TokenType, &allowedTokens)) then false else

        let expr: IFSharpExpression = getNode textControl lexer.TokenStart
        if isNull expr then false else

        let tokenBeforeKeywordEnd = advanceToPrevToken lexer

        let indentOffset =
            let tryGetIndentOffset (exprBeforeKeyword: IFSharpExpression) (indentAnchor: ITreeNode) =
                if isNotNull exprBeforeKeyword && isNotNull indentAnchor &&
                   exprBeforeKeyword.GetDocumentEndOffset().Offset = tokenBeforeKeywordEnd then
                   Some (indentAnchor.GetDocumentStartOffset().Offset)
                else None

            match expr with
            // if expr then ...
            // elif ... then {caret}
            | :? IElifExpr as elifExpr ->
                let ifExpr = IfThenElseExprNavigator.GetByElseExpr(elifExpr)
                if isNull ifExpr then None else
                tryGetIndentOffset elifExpr.ConditionExpr ifExpr.ConditionExpr

            // if expr then {caret}
            // No info is available after error recovery, we check that range is surrounded with if ... then.
            | :? IIfThenElseExpr as ifThenElseExpr ->
                let conditionExpr = ifThenElseExpr.ConditionExpr
                tryGetIndentOffset conditionExpr conditionExpr

            // while expr do {caret}
            | :? IWhileExpr as whileExpr ->
                let expr = whileExpr.ConditionExpr
                tryGetIndentOffset expr expr

            // for i = ... to ... do {caret}
            | :? IForExpr as forExpr ->
                tryGetIndentOffset forExpr.ToExpression forExpr.Identifier

            // for ... in ... do {caret}
            | :? IForEachExpr as foreachExpr ->
                tryGetIndentOffset foreachExpr.InExpression foreachExpr.Pattern

            | _ -> None

        match indentOffset with
        | None -> false
        | Some exprStart ->

        use cookie = LexerStateCookie.Create(lexer)
        if exprStart <= 0 || not (lexer.FindTokenAt(exprStart - 1)) then false else

        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance(-1)

        if not (Array.contains lexer.TokenType allowedTokens) then false else

        let indent =
            let document = textControl.Document
            let tokenLine = document.GetCoordsByOffset(lexer.TokenStart).Line
            let offsetInLine = getOffsetInLine document tokenLine lexer.TokenStart
            offsetInLine + getIndentSize textControl

        insertNewLineAt textControl indent TrimTrailingSpaces.Yes

    member x.IndentAfterRArrow(textControl: ITextControl, lexer: CachingLexer) =
        if lexer.TokenType != FSharpTokenType.RARROW then false else

        let matchClause: IMatchClause = getNode textControl lexer.TokenStart
        if isNull matchClause then false else

        let whenExpression = matchClause.WhenExpression
        let offset =
            if isNull whenExpression then matchClause.Pattern.GetDocumentEndOffset()
            else whenExpression.GetDocumentEndOffset()

        let prevTokenEnd = advanceToPrevToken lexer
        if offset.Offset <> prevTokenEnd then false else

        use cookie = LexerStateCookie.Create(lexer)

        if not (lexer.FindTokenAt(matchClause.GetDocumentStartOffset().Offset)) then false else
        if lexer.TokenType != FSharpTokenType.BAR then false else

        let indent =
            let document = textControl.Document
            let tokenLine = document.GetCoordsByOffset(lexer.TokenStart).Line
            let offsetInLine = getOffsetInLine document tokenLine lexer.TokenStart
            offsetInLine + getIndentSize textControl

        insertNewLineAt textControl indent TrimTrailingSpaces.Yes

    member x.HandleEnterInsideSingleLineBrackets(textControl: ITextControl, lexer: CachingLexer, line) =
        use cookie = LexerStateCookie.Create(lexer)

        let document = textControl.Document
        let lambdaExpr =
            if lexer.TokenType != FSharpTokenType.RARROW then null else
            let settingsStore = x.SettingsStore.BindToContextTransient(textControl.ToContextRange())
            if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.MultiLineLambdaClosingNewline) then
                getLambdaExprFromRarrow lexer.TokenStart textControl dependencies.Solution else null

        let tokenType, leftBracketStartOffset, leftBracketEndOffset =
            if isNotNull lambdaExpr && isNotNull lambdaExpr.Expression then
                let rarrowStartOffset = lexer.TokenStart
                let rarrowEndOffset = lexer.TokenEnd
                let parenExpr = ParenExprNavigator.GetByInnerExpression(lambdaExpr)
                if isNotNull parenExpr && isNotNull parenExpr.LeftParen then
                    lexer.FindTokenAt(parenExpr.LeftParen.GetDocumentStartOffset().Offset) |> ignore

                let lparenTokenType = lexer.TokenType
                lparenTokenType, rarrowStartOffset, rarrowEndOffset
            else
                lexer.TokenType, lexer.TokenStart, lexer.TokenEnd

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

        use command = x.CommandProcessor.UsingCommand("New Line")
        let baseIndentLength =
            if not shouldDeindent then
                getOffsetInLine document line leftBracketStartOffset
            else
                let line = getContinuedIndentLine textControl leftBracketStartOffset LeadingParenContinuesLine.Yes
                getLineWhitespaceIndent textControl line

        let baseIndentString = x.GetNewLineText(textControl) + String(' ', baseIndentLength)
        let indentString = baseIndentString + String(' ',  getIndentSize textControl)

        if lastElementEndOffset = leftBracketEndOffset then
            let newText =
                if tokenType == FSharpTokenType.LPAREN then
                    indentString
                else
                    indentString + baseIndentString

            document.ReplaceText(TextRange(lastElementEndOffset, rightBracketStartOffset), newText)
        else
            let firstElementStartOffset =
                lexer.FindTokenAt(leftBracketEndOffset) |> ignore
                while isIgnored lexer.TokenType do
                    lexer.Advance()
                lexer.TokenStart

            if tokenType != FSharpTokenType.LPAREN || isNotNull lambdaExpr then
                document.ReplaceText(TextRange(lastElementEndOffset, rightBracketStartOffset), baseIndentString)

            document.ReplaceText(TextRange(leftBracketEndOffset, firstElementStartOffset), indentString)

        textControl.Caret.MoveTo(leftBracketEndOffset + indentString.Length, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleEnterInEmptyLambda(textControl: ITextControl, lexer: CachingLexer) =
        let settingsStore = x.SettingsStore.BindToContextTransient(textControl.ToContextRange())
        if not (settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.MultiLineLambdaClosingNewline)) then
            false else

        use cookie = LexerStateCookie.Create(lexer)

        let tokenType = lexer.TokenType

        let nextTokenType =
            use cookie = LexerStateCookie.Create(lexer)
            lexer.Advance()
            while isIgnored lexer.TokenType do
                lexer.Advance()
            lexer.TokenType

        if tokenType != FSharpTokenType.RARROW || nextTokenType != FSharpTokenType.RPAREN then false else

        let lambdaExpr = getLambdaExprFromRarrow lexer.TokenStart textControl dependencies.Solution
        if isNull lambdaExpr then false else

        lexer.Advance()
        while isIgnored lexer.TokenType do
            lexer.Advance()

        if not (FSharpBracketMatcher().FindMatchingBracket(lexer)) then false else

        let document = textControl.Document
        let leftBracketStartOffset = lexer.TokenStart
        let leftBracketLine = document.GetCoordsByOffset(leftBracketStartOffset).Line
        if document.GetCoordsByOffset(lexer.TokenStart).Line <> leftBracketLine then false else

        let defaultIndent = getIndentSize textControl
        let line = getContinuedIndentLine textControl leftBracketStartOffset LeadingParenContinuesLine.No
        match getLineIndent dependencies.CachingLexerService textControl line with
        | None -> false
        | Some indent ->

        let indentSize = defaultIndent + indent.Indent
        insertNewLineAt textControl indentSize TrimTrailingSpaces.Yes |> ignore
        let pos = textControl.Caret.Position.Value.ToDocOffset()

        insertNewLineAt textControl indent.Indent TrimTrailingSpaces.Yes |> ignore

        textControl.Caret.MoveTo(pos, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandlerEnterInTripleQuotedString(textControl: ITextControl) =
        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)

        if not (isNotNull lexer && lexer.FindTokenAt(offset)) then false else

         // """{caret} foo"""
        if lexer.TokenType != FSharpTokenType.TRIPLE_QUOTED_STRING then false else
        if offset < lexer.TokenStart + 3 || offset > lexer.TokenEnd - 3 then false else

        let document = textControl.Document
        let strStartLine = document.GetCoordsByOffset(lexer.TokenStart).Line
        let strEndLine = document.GetCoordsByOffset(lexer.TokenEnd).Line
        if strStartLine <> strEndLine then false else

        use command = x.CommandProcessor.UsingCommand("New Line")
        let newLineString = x.GetNewLineText(textControl)
        document.InsertText(lexer.TokenEnd - 3, newLineString)
        document.InsertText(offset, newLineString)
        textControl.Caret.MoveTo(offset + newLineString.Length, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleEnterAfterSingleLineIf(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else
        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(offset)) then false else

        let prevTokenEnd = advanceToPrevToken lexer
        let node = getNode textControl (prevTokenEnd - 1)
        if getTokenType node != FSharpTokenType.ELSE then false else

        let ifExpr = node.Parent.As<IIfExpr>()
        if isNull ifExpr then false else

        let ifKeyword = ifExpr.IfKeyword
        if isNull ifKeyword then false else

        let line = node.StartLine
        if line <> ifKeyword.StartLine && line <> ifExpr.ThenExpr.StartLine then false else

        insertNewLineAt textControl ifKeyword.Indent TrimTrailingSpaces.Yes

    member x.HandleEnterInApp(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else

        if not (lexer.FindTokenAt(offset - 1) &&
                let tokenType = lexer.TokenType
                (tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.RPAREN) ||

                lexer.FindTokenAt(offset) &&
                let tokenType = lexer.TokenType
                (isNotNull tokenType && tokenType.IsWhitespace || tokenType == FSharpTokenType.LPAREN)) then false else

        lexer.FindTokenAt(offset - 1) |> ignore

        let isAfterInfixOp =
            use cookie = LexerStateCookie.Create(lexer)
            while lexer.TokenType == FSharpTokenType.WHITESPACE do
                lexer.Advance(-1)
            isInfixOp lexer

        let isBeforeInfixOp =
            use cookie = LexerStateCookie.Create(lexer)
            lexer.FindTokenAt(offset) |> ignore
            while lexer.TokenType == FSharpTokenType.WHITESPACE do
                lexer.Advance()
            isInfixOp lexer

        let document = textControl.Document
        let caretCoords = document.GetCoordsByOffset(offset)
        let caretLine = caretCoords.Line

        let lineStart = document.GetLineStartOffset(caretLine)
        if document.GetText(TextRange(lineStart, offset)).IsWhitespace() then false else

        match x.GetFSharpTree(textControl) with
        | None -> false
        | Some parseTree ->

        let mutable appExpr = None
        let mutable outerExpr = None
        let visitor =
            { new SyntaxVisitorBase<_>() with
                member x.VisitExpr(_, _, defaultTraverse, expr) =
                    match expr with
                    | SynExpr.App (_, false, leftExpr, rightExpr, range)
                    | SynExpr.App (_, true,  rightExpr, leftExpr, range) ->

                        if offset >= getStartOffset document range && rightExpr.Range.GetStartLine() > caretLine then
                            match leftExpr, expr with
                            | _, SynExpr.App(_, true,  rightExpr, leftExpr, _)
                            | SynExpr.App (_, true, rightExpr, leftExpr, _), _ when
                                    leftExpr.Range.EndLine = rightExpr.Range.StartLine -> ()
                            | _ -> outerExpr <- Some (expr, rightExpr)

                        if offset >= getEndOffset document leftExpr.Range &&
                                offset <= getStartOffset document rightExpr.Range then
                            appExpr <- Some expr

                            outerExpr <-
                                match outerExpr with
                                | Some (expr, _) when range.Start = expr.Range.Start -> outerExpr
                                | _ -> None

                        defaultTraverse expr

                    | _ -> defaultTraverse expr }

        SyntaxTraversal.Traverse(getPosFromCoords caretCoords, parseTree, visitor) |> ignore
        match appExpr, outerExpr with
        | None, _ -> false
        | Some expr, None
        | _, Some (_, expr) ->

        let indent =
            match expr with
            | SynExpr.App (_, (true  as isInfix), _, argExpr, _)
            | SynExpr.App (_, (false as isInfix), argExpr, _, _) when
                isInfix && isBeforeInfixOp || not isInfix && isAfterInfixOp ->
                let argExprLine = argExpr.Range.GetStartLine()
                getOffsetInLine document argExprLine (getStartOffset document expr.Range)

            | _ ->

            let exprStartLine = expr.Range.GetStartLine()

            if exprStartLine < caretLine then
                getLineWhitespaceIndent textControl caretLine else

            let offsetInLine = getOffsetInLine document exprStartLine (getStartOffset document expr.Range)
            if exprStartLine > caretLine then offsetInLine else

            let additionalIndent = getIndentSize textControl
            offsetInLine + additionalIndent

        insertNewLineAt textControl indent TrimTrailingSpaces.Yes

    member x.HandleEnterAfterErrorAndInfixOp(textControl: ITextControl) =
        let lexer = x.GetCachingLexer(textControl)
        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else
        if not (isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else

        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance(-1)

        if not (isInfixOp lexer) then false else

        let opStartOffset = lexer.TokenStart
        let opEndOffset = lexer.TokenEnd

        let nextTokenIsKeyword =
            use cookie = LexerStateCookie.Create(lexer)
            lexer.Advance()

            while lexer.TokenType == FSharpTokenType.WHITESPACE do
                lexer.Advance()

            isNotNull lexer.TokenType && lexer.TokenType.IsKeyword

        lexer.Advance(-1)
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance(-1)

        let tokenType = lexer.TokenType
        if isNull tokenType then false else

        let prevEndOffset =
            if tokenType == FSharpTokenType.NEW_LINE then None else
            Some lexer.TokenEnd

        let opRefExpr: IReferenceExpr = getNode textControl opStartOffset
        let binaryExpr = BinaryAppExprNavigator.GetByOperator(opRefExpr)
        if isNull binaryExpr || not (binaryExpr.RightArgument :? IFromErrorExpr) then false else

        let expr =
            match prevEndOffset with
            | None -> opRefExpr :> IFSharpExpression
            | Some prevEndOffset ->

            let expr = binaryExpr.LeftArgument
            if isNull expr then null else
            let offset = expr.GetDocumentEndOffset().Offset
            if offset = prevEndOffset then expr else null

        if isNull expr then false else

        let indent =
            let startLine = expr.StartLine
            let endLine = expr.EndLine

            if startLine <> endLine then getLineWhitespaceIndent textControl endLine else

            let offset = expr.GetDocumentStartOffset().Offset
            getOffsetInLine textControl.Document startLine offset

        use command = x.CommandProcessor.UsingCommand("New Line")
        if not (insertNewLineAt textControl indent TrimTrailingSpaces.Yes) then false else

        if nextTokenIsKeyword then
            textControl.Document.InsertText(textControl.Caret.Offset(), " ")
            textControl.Caret.MoveTo(textControl.Caret.Offset() - 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleEnterBeforeDot(textControl: ITextControl) =
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else

        if not (lexer.FindTokenAt(offset)) then false else
        while lexer.TokenType == FSharpTokenType.WHITESPACE do
            lexer.Advance()

        if lexer.TokenType != FSharpTokenType.DOT then false else
        let dotOffset = lexer.TokenStart

        let document = textControl.Document
        let caretCoords = document.GetCoordsByOffset(offset)
        let caretLine = caretCoords.Line

        let qualifiedExpr: IQualifiedExpr = getNode textControl offset
        if isNull qualifiedExpr then false else

        if qualifiedExpr.StartLine <> caretLine then false else

        let delimiter = qualifiedExpr.Delimiter
        if isNull delimiter || delimiter.GetDocumentStartOffset().Offset <> dotOffset then false else

        let exprOffset = qualifiedExpr.GetDocumentStartOffset().Offset

        let indent =
            getOffsetInLine document caretLine exprOffset +
            getIndentSize textControl

        insertNewLineAt textControl indent TrimTrailingSpaces.Yes

    member x.HandleBackspaceInInterpolatedString(context: IActionContext) =
      let textControl = context.TextControl
      let lexer = x.GetCachingLexer(textControl)
      let charPos = textControl.Caret.Offset()
      if charPos <= 0 || not (isNotNull lexer && lexer.FindTokenAt(charPos)) then false else

      if lexer.TokenStart <> charPos then false else
      let tokenType = lexer.TokenType
      if tokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE &&
         tokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_END &&
         tokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE &&
         tokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END &&
         tokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE &&
         tokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END
            then false else

      lexer.Advance(-1)
      let prevTokenType = lexer.TokenType
      if prevTokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_START &&
         prevTokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE &&
         prevTokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START &&
         prevTokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE &&
         prevTokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START &&
         prevTokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE
            then false else

      textControl.Caret.MoveTo(charPos - 1, CaretVisualPlacement.DontScrollIfVisible)
      textControl.Document.DeleteText(TextRange(charPos - 1, charPos + 1))
      true

    member x.HandleBackspacePressed(context: IActionContext) =
        let textControl = context.TextControl
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        if this.HandleBackspaceInInterpolatedString(context) then true else
        if this.HandleBackspaceInTripleQuotedString(textControl) then true else

        this.DoHandleBackspacePressed
            (textControl,
             (fun tokenType ->
                tokenType == FSharpTokenType.STRING ||
                tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
                tokenType == FSharpTokenType.VERBATIM_STRING ||
                tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING),
             (fun _ -> FSharpBracketMatcher() :> _))

    member x.HandleBackspaceInTripleQuotedString(textControl: ITextControl) =
        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)

        let isTripleQuoteString (tokenType: TokenNodeType) =
            tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING ||
            tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING

        if not (isNotNull lexer && lexer.FindTokenAt(offset)) then false else
        if not (isTripleQuoteString lexer.TokenType) || lexer.TokenStart = offset then false else

        let getStrStart (tokenType: TokenNodeType) =
            if tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then "\"\"\"" else
            if tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING then "$\"\"\"" else

            failwithf "Unexpected token %O" tokenType

        let strStart = getStrStart lexer.TokenType
        let strEnd = "\"\"\""

        let newLineLength = this.GetNewLineText(textControl).Length

        // """{caret}"""
        if lexer.TokenStart = offset - strStart.Length && lexer.TokenEnd = offset + strEnd.Length then
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

            let lastNewLineOffset = lexer.TokenEnd - strEnd.Length - newLineLength
            textControl.Document.DeleteText(TextRange(lastNewLineOffset, lastNewLineOffset + newLineLength))
            textControl.Document.DeleteText(TextRange(offset - newLineLength, offset))
            textControl.Caret.MoveTo(offset - newLineLength, CaretVisualPlacement.DontScrollIfVisible)
            true

        else false

    member x.HandleSurroundTyping(typingContext, lChar, rChar, lTokenType, rTokenType, shouldNotSurround) =
        base.HandleSurroundTyping(typingContext, lChar, rChar, lTokenType, rTokenType, shouldNotSurround)

    member x.TrySurroundWithBraces(context, typedBracket, typedBracketIsLeft, matchingBrackets: IDictionary<_, _>) =
        let mutable secondBracket = Unchecked.defaultof<_>
        if not (matchingBrackets.TryGetValue(typedBracket, &secondBracket)) then false else

        let lBrace, rBrace = if typedBracketIsLeft then typedBracket, secondBracket else secondBracket, typedBracket
        let lToken, rToken = bracketToTokenType[lBrace], bracketToTokenType[rBrace]
        x.HandleSurroundTyping(context, lBrace, rBrace, lToken, rToken, JetFunc<_>.False)

    member x.HandleLeftBracket(context: ITypingContext) =
        let hasSelection = context.TextControl.Selection.HasSelection()

        this.HandleLeftBracketTyped(context,
             (fun lexer -> leftBrackets.Contains(lexer.TokenType)),
             (fun _ -> FSharpBracketMatcher() :> _),
             (fun _ -> x.TrySurroundWithBraces(context, context.Char, true, leftToRightBracket)),
             (fun tokenType _ -> tokensSuitableForRightBracket.Contains(tokenType)),
             (fun _ -> rightBracketsText[context.Char])) |> ignore

        // todo: insert pair brace in `$"{ {caret} }"`

        if context.Char = '{' && not hasSelection then
            let textControl = context.TextControl
            let lexer = x.GetCachingLexer(textControl)
            let typedPos = textControl.Caret.Offset() - 1
            if isNotNull lexer && lexer.FindTokenAt(typedPos) then
                base.AutoInsertRBraceInStringInterpolation(textControl, lexer, typedPos, interpolatedStrings,
                    skipCloseBrackets, FSharpTokenType.RBRACE) |> ignore

        true

    member x.HandleRightBracket(context: ITypingContext) =
        let textControl = context.TextControl
        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else

        if x.SkipCharInRightBracket(context, lexer, offset) then true else
        if context.Char = ')' && x.SkipCharInOperator(context, lexer, offset) then true else

        this.HandleRightBracketTyped
            (context,
             (fun _ -> x.TrySurroundWithBraces(context, context.Char, false, rightToLeftBracket)),
             (fun tokenType -> tokenType == FSharpTokenType.WHITESPACE),
             (fun lexer -> x.NeedSkipCloseBracket(context.TextControl, lexer, context.Char)))

    member x.NeedSkipCloseBracket(textControl, lexer, charTyped) =
        x.NeedSkipCloseBracket(textControl, lexer, charTyped, bracketTypesForRightBracketChar,
            JetFunc<_,_>.False, (fun _ -> FSharpSkipPairBracketsMatcher() :> _))

    member x.HandleSingleQuoteTyped(context: ITypingContext) =
        if context.EnsureWritable() <> EnsureWritableResult.SUCCESS then false else
        if context.TextControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        x.SkipQuote(context.TextControl, context.Char)

    member x.HandleQuoteTyped(context: ITypingContext) =
        let textControl = context.TextControl
        let typedChar = context.Char

        if context.EnsureWritable() <> EnsureWritableResult.SUCCESS then false else

        let selection = textControl.Selection

        let surroundTypingEnabled =
            let settingsStore = x.SettingsStore.BindToContextTransient(textControl.ToContextRange())
            settingsStore.GetValue(TypingAssistOptions.SurroundTypingEnabled)

        if surroundTypingEnabled && selection.HasSelection() && typedChar = '"' then
            x.SurroundSelectionWithQuotes(textControl, fun _ -> false)
            true else

        if selection.OneDocRangeWithCaret().Length > 0 then false else

        if x.HandleThirdQuote(textControl, typedChar) then true else
        if x.SkipQuote(textControl, typedChar) then true else

        x.InsertPairQuote(textControl, typedChar)

    member x.HandleBacktickTyped(context: ITypingContext) =
        let textControl = context.TextControl
        let offset = textControl.Caret.Offset()

        let lexer = x.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else

        if x.FinishBacktickedId(textControl, lexer, offset) then true else
        if x.SkipBacktickInId(textControl, lexer, offset) then true else

        false

    member x.FinishBacktickedId(textControl: ITextControl, lexer, offset) =
        if not (isBacktick lexer) then false else
        if prevTokenIs isBacktick lexer || nextTokenIs isBacktick lexer then false else

        if lexer.FindTokenAt(offset) &&
                (lexer.TokenType == FSharpTokenType.IDENTIFIER || lexer.TokenType.IsKeyword) then
            textControl.Document.InsertText(lexer.TokenEnd, "``")
            textControl.Document.InsertText(offset, "`")
        else
            textControl.Document.InsertText(offset, "```")

        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.SkipBacktickInId(textControl: ITextControl, lexer: CachingLexer, offset) =
        if not (lexer.FindTokenAt(offset)) then false else
        if lexer.TokenType != FSharpTokenType.IDENTIFIER then false else
        if offset + 2 < lexer.TokenEnd then false else
        if not (lexer.GetTokenText().IsEscapedWithBackticks()) then false else

        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.SkipQuote(textControl: ITextControl, typedChar: char) =
        let buffer = textControl.Document.Buffer
        let offset = textControl.Caret.Offset()
        if offset >= buffer.Length || typedChar <> buffer[offset] then false else

        let skipEndQuote (lexer: CachingLexer) =
            typedChar = getStringEndingQuote lexer.TokenType &&

            let endingQuotesLength = getDefaultStringEndingQuotesLength lexer.TokenType
            endingQuotesLength > 0 && offset >= lexer.TokenEnd - endingQuotesLength

        let skipEscapedQuoteInVerbatim (lexer: CachingLexer) =
            lexer.TokenType == FSharpTokenType.VERBATIM_STRING && typedChar = '\"' // todo: isv

        let lexer = x.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else
        if not strings[lexer.TokenType] || offset = lexer.TokenEnd then false else
        if not (skipEndQuote lexer || skipEscapedQuoteInVerbatim lexer) then false else

        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.InsertPairQuote(textControl: ITextControl, typedChar: char) =
        let offset = textControl.Caret.Offset()
        let document = textControl.Document
        let line = document.GetCoordsByOffset(offset).Line

        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else

        if offset = document.GetDocumentEndOffset().Offset then
            if lexer.FindTokenAt(offset - 1) &&
               lexer.TokenType == FSharpTokenType.UNFINISHED_TRIPLE_QUOTED_STRING then false else

            textControl.Document.InsertText(offset, emptyString)
            textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
            true
        else

        if lexer.FindTokenAt(offset - 1) && lexer.TokenType.IsComment then false else
        if not (lexer.FindTokenAt(offset)) then false else

        let tokenType = lexer.TokenType
        if tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then false else

        if tokenType == FSharpTokenType.STRING && typedChar = '\"' &&
               FSharpTypingAssist.EscapesNextChar(offset, textControl.Document.Buffer) then false else

        // Do not prevent quoting code
        if not (isStringLiteralStopper tokenType) then false else

        // Workaround for cases like { name = "{caret}, title = "" }
        let tokenStartLine = document.GetCoordsByOffset(lexer.TokenStart).Line
        if tokenType == FSharpTokenType.STRING && offset > lexer.TokenStart &&
           tokenStartLine = line && lineEndsWithString lexer document line then false else

        textControl.Document.InsertText(offset, emptyString)
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)

        true

    member x.HandleThirdQuote(textControl: ITextControl, typedChar: char) =
        if typedChar <> '\"' then false else

        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)

        let isEmptyRegularString (lexer: CachingLexer) =
            lexer.TokenType == FSharpTokenType.STRING && lexer.GetTokenLength() = 2 ||
            lexer.TokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING && lexer.GetTokenLength() = 3

        if not (isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else
        if not (isEmptyRegularString lexer) then false else
        if lexer.TokenEnd <> offset then false else

        textControl.Document.InsertText(offset, "\"\"\"\"")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleSpaceInsideEmptyBrackets(textControl: ITextControl) =
        let lexer = x.GetCachingLexer(textControl)

        let isAvailable =
            x.CheckAndDeleteSelectionIfNeeded(textControl, fun selection ->
                let offset = selection.StartOffset.Offset
                if not (offset > 0 && isNotNull lexer) then false else

                if not (lexer.FindTokenAt(offset - 1)) then false else
                let left = lexer.TokenType

                if not (lexer.FindTokenAt(offset)) then false else
                let right = lexer.TokenType

                emptyBracketsToAddSpace.Contains((left, right)) || isInsideEmptyQuotation lexer offset)

        if not isAvailable then false else

        let offset = textControl.Caret.Offset()
        textControl.Document.InsertText(offset, "  ")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleRightAngleBracketTyped(context: ITypingContext) =
        let lexer = x.GetCachingLexer(context.TextControl)
        if isNull lexer then false else

        x.IsTypingSmartParenthesisHandlerAvailable2(context) && x.HandleAngleBracketsInList(context, lexer)
        || x.HandleXmlDocTag(context, lexer)

    member x.HandleAngleBracketsInList(context, lexer: CachingLexer) =
        insertCharInBrackets context lexer angledBracketsChars listBrackets LeftBracketOnly.Yes

    member x.HandleBarTyped(context: ITypingContext) =
        let textControl = context.TextControl
        let offset = textControl.Caret.Offset()
        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else

        if x.SkipCharInRightBracket(context, lexer, offset) then true else

        insertCharInBrackets context lexer barChars listBrackets LeftBracketOnly.Yes ||
        insertCharInBrackets context lexer barChars recordBrackets LeftBracketOnly.Yes

    member x.SkipCharInRightBracket(context, lexer: CachingLexer, offset) =
        use cookie = LexerStateCookie.Create(lexer)
        if not (lexer.FindTokenAt(offset)) then false else

        let mutable offsetInTokens = Unchecked.defaultof<_>
        if not (charOffsetInRightBrackets.TryGetValue(context.Char, &offsetInTokens)) then false else

        let offsetInToken = offset - lexer.TokenStart
        if not (Array.contains (lexer.TokenType, offsetInToken) offsetInTokens) then false else

        context.TextControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.SkipCharInOperator(context, lexer: CachingLexer, offset) =
        use cookie = LexerStateCookie.Create(lexer)
        if not (lexer.FindTokenAt(offset)) then false else

        if lexer.TokenType != FSharpTokenType.RPAREN then false else

        lexer.Advance(-1)
        if lexer.TokenType != FSharpTokenType.LESS && lexer.TokenType != FSharpTokenType.GREATER then false else

        lexer.Advance(-1)
        if lexer.TokenType != FSharpTokenType.LPAREN then false else

        context.TextControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.HandleAtTyped(context: ITypingContext) =
        let textControl = context.TextControl
        if textControl.Selection.OneDocRangeWithCaret().Length > 0 then false else

        let offset = textControl.Caret.Offset()
        if offset <= 0 then false else

        let lexer = x.GetCachingLexer(textControl)
        if not (isNotNull lexer && lexer.FindTokenAt(offset - 1)) then false else

        if x.SkipCharInRightBracket(context, lexer, offset) then true else
        if x.MakeQuotation(textControl, lexer, offset) then true else
        x.MakeEmptyQuotationUntyped(context, lexer, offset)

    member x.MakeQuotation(textControl: ITextControl, lexer: CachingLexer, offset) =
        if lexer.TokenType != FSharpTokenType.LESS then false else

        textControl.Document.InsertText(offset, "@@>")
        textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
        true

    member x.MakeEmptyQuotationUntyped(context: ITypingContext, lexer: CachingLexer, offset) =
        let textControl = context.TextControl

        if isInsideEmptyQuotation lexer offset && lexer.GetTokenLength() = 4 then
            textControl.Document.InsertText(offset, "@@")
            textControl.Caret.MoveTo(offset + 1, CaretVisualPlacement.DontScrollIfVisible)
            true else

        insertCharInBrackets context lexer atChars typedQuotationBrackets LeftBracketOnly.No

    member x.HandleXmlDocTag(context: ITypingContext, lexer) =
        let textControl = context.TextControl
        let document = textControl.Document
        let offset = textControl.Caret.Offset()
        let buffer = lexer.Buffer

        if offset < 3 then false else

        if not (lexer.FindTokenAt(offset - 1)) then false else
        if not (lexer.TokenType == FSharpTokenType.LINE_COMMENT) then false else

        let tokenLength = lexer.TokenEnd - lexer.TokenStart + 1

        if tokenLength < 3 ||
           // instead of ///
           buffer[lexer.TokenStart + 2] <> '/' ||
           //// instead of ///
           tokenLength >= 4 && buffer[lexer.TokenStart + 3] = '/' then false else

        let mutable spaceCounter = lexer.TokenStart + 3
        while buffer[spaceCounter] = ' ' && spaceCounter < lexer.TokenEnd do
            spaceCounter <- spaceCounter + 1

        if spaceCounter <> lexer.TokenEnd then false else

        context.CallNext()
        let file = x.CommitPsiOnlyAndProceedWithDirtyCaches(textControl, id)

        let tokenNode = file.FindTokenAt(TreeOffset(offset)).As<DocComment>()
        if isNull tokenNode then true else

        let docCommentBlockNode = tokenNode.Parent.As<IDocCommentBlock>()

        if isNull docCommentBlockNode || not docCommentBlockNode.IsSingleLine then true else

        let docCommentBlockOffset = docCommentBlockNode.GetDocumentStartOffset()
        let coords = docCommentBlockOffset.ToDocumentCoords()
        let docCommentBlockLine = coords.Line

        let newLine = x.GetNewLineText(textControl)
        let lineStart = document.GetLineStartDocumentOffset(docCommentBlockLine)
        let range = DocumentRange(&lineStart, &docCommentBlockOffset)
        let indent = range.GetText()
        let templateLinePrefix = indent + "/// "

        let struct(template, caretOffset) =
            XmlDocTemplateUtil.GetDocTemplate(docCommentBlockNode, templateLinePrefix, newLine);

        context.QueueCommand(fun _ ->
            use _ = this.CommandProcessor.UsingCommand("Insert XmlDoc template")
            let endOffset = docCommentBlockNode.GetDocumentEndOffset()
            document.DeleteText(DocumentRange(&lineStart, &endOffset))
            document.InsertText(lineStart, template)
            textControl.Caret.MoveTo(lineStart + caretOffset - newLine.Length, CaretVisualPlacement.DontScrollIfVisible))

        true

    member x.TrimTrailingSpacesAtOffset(textControl: ITextControl, startOffset: byref<int>, trimAfterCaret) =
        let isWhitespace c =
            c = ' ' || c = '\t'

        let document = textControl.Document
        let line = document.GetCoordsByOffset(startOffset).Line
        let lineStart = document.GetLineStartOffset(line)
        if document.GetText(TextRange(lineStart, startOffset)).IsWhitespace() then () else

        let mutable endOffset = startOffset
        let buffer = document.Buffer

        let rec skipWhitespaceBefore offset =
            if offset > 0 && isWhitespace buffer[offset - 1] then
                skipWhitespaceBefore (offset - 1)
            else offset

        startOffset <- skipWhitespaceBefore startOffset

        if startOffset > 0 && buffer[startOffset - 1] = ';' then
            let settingsStore = x.SettingsStore.BindToContextTransient(textControl.ToContextRange())
            if not (settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SemicolonAtEndOfLine)) then
                let fsFile = textControl.GetFSharpFile(dependencies.Solution)
                let token = fsFile.FindTokenAt(DocumentOffset(document, startOffset - 1))
                if isNull token || getTokenType token <> FSharpTokenType.SEMICOLON then () else

                // No offside rule in attribute lists, dotnet/fsharp#7752
                if token.Parent :? IAttributeList then () else

                startOffset <- startOffset - 1

            startOffset <- skipWhitespaceBefore startOffset

        let lineEndOffset = document.GetLineEndOffsetNoLineBreak(line)
        if trimAfterCaret = TrimTrailingSpaces.Yes then
            while endOffset < lineEndOffset && isWhitespace buffer[endOffset] do
                endOffset <- endOffset + 1

        let additionalSpaces =
            if endOffset >= lineEndOffset then 0 else
            getAdditionalSpacesBeforeToken textControl endOffset lineStart

        if additionalSpaces > 0 then
            let replaceRange = TextRange(startOffset, endOffset)
            document.ReplaceText(replaceRange, String(' ', additionalSpaces))

        elif startOffset <> endOffset then
            document.DeleteText(TextRange(startOffset, endOffset))

    member x.GetNewLineText(textControl: ITextControl) =
        x.GetNewLineText(textControl.Document.GetPsiSourceFile(x.Solution))

    member x.IsActionHandlerAvailable2(context) = base.IsActionHandlerAvailable(context)
    member x.IsTypingHandlerAvailable2(context) = base.IsTypingHandlerAvailable(context)

    member x.IsTypingSmartParenthesisHandlerAvailable2(context) =
        let textControl = context.TextControl
        let offset = textControl.Caret.Offset()

        let lexer = x.GetCachingLexer(textControl)
        if isNull lexer then false else
        if offset > 0 && not (lexer.FindTokenAt(offset - 1)) then false else

        // Don't add pair quotes/brackets after opening char quote:
        // `'"{caret}"` or `'({caret})`
        if lexer.TokenType = FSharpTokenType.QUOTE && lexer.GetTokenLength() = 1 then false else

        base.IsTypingSmartParenthesisHandlerAvailable(context)

    member x.GetFSharpTree(textControl: ITextControl) =
        match x.CommitPsiOnlyAndProceedWithDirtyCaches(textControl, id).AsFSharpFile() with
        | null -> None
        | fsFile -> fsFile.ParseTree

    override x.IsSupported(textControl: ITextControl) =
        match textControl.Document.GetPsiSourceFile(x.Solution) with
        | null -> false
        | sourceFile ->

        sourceFile.IsValid() &&
        sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() &&
        sourceFile.Properties.ProvidesCodeModel

    interface ITypingHandler with
        member x.QuickCheckAvailability(_, sourceFile) =
            sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>()


type LineIndent =
    // Code indent, as seen by compiler.
    | Source of int

    // Fallback indent when no code is present on line. Used to guess the desired indentation.
    | Comments of int

    member this.Indent =
        match this with
        | Source indent
        | Comments indent -> indent

let getLineIndent (cachingLexerService: CachingLexerService) (textControl: ITextControl) (line: Line) : LineIndent option =
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
        tryGetNestedIndentBelow cachingLexerService textControl line false currentIndent

let tryGetNestedIndentBelow cachingLexerService textControl line preferComment currentIndent =
    let linesCount = textControl.Document.GetLineCount()

    let rec tryFindIndent (firstFoundCommentIndent: (Line * LineIndent) option) line =
        if line >= linesCount then firstFoundCommentIndent else

        let indent =
            getLineIndent cachingLexerService textControl line
            |> Option.map (fun indent -> line, indent)

        match indent, firstFoundCommentIndent with
        | Some (_, Comments _), _ when preferComment -> indent

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
        if FSharpTokenType.RightBraces[lexer.TokenType] then
            if matcher.FindMatchingBracket(lexer) then
                lexer.Advance(-1)

        if FSharpTokenType.LeftBraces[lexer.TokenType] then
            foundToken <- true

        elif not FSharpTokenType.RightBraces[lexer.TokenType] then
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

let getLambdaExprFromRarrow offset (textControl: ITextControl) (solution: ISolution) : ILambdaExpr =
    let file = textControl.GetFSharpFile(solution)
    let node = file.FindTokenAt(TreeOffset(offset))
    if isNull node then null else

    let parent = node.Parent.As<ILambdaExpr>()
    if isNotNull parent && parent.RArrow == node then parent else null

let lineEndsWithString (lexer: CachingLexer) (document: IDocument) line =
    use cookie = LexerStateCookie.Create(lexer)
    let lineEnd = document.GetLineEndOffsetNoLineBreak(line)
    if not (lexer.FindTokenAt(lineEnd)) then false else

    let tokenType = lexer.TokenType
    tokenType == FSharpTokenType.STRING || tokenType == FSharpTokenType.UNFINISHED_STRING


let shouldTrimSpacesBeforeToken (tokenType: TokenNodeType) =
    if isNull tokenType || FSharpTokenType.RightBraces[tokenType] || tokenType.IsComment then TrimTrailingSpaces.No
    else TrimTrailingSpaces.Yes


type FSharpBracketMatcher private (brackets) =
    inherit BracketMatcher(brackets)

    static let matchingBrackets =
        [| Pair.Of(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN)
           Pair.Of(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK)
           Pair.Of(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE)
           Pair.Of(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK)
           Pair.Of(FSharpTokenType.LBRACE_BAR, FSharpTokenType.BAR_RBRACE)
           Pair.Of(FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK)
           Pair.Of(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED)
           Pair.Of(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED) |]

    new () =
        FSharpBracketMatcher(matchingBrackets)


type FSharpSkipPairBracketsMatcher private (brackets) =
    inherit BracketMatcher(brackets)

    static let skipPairBrackets =
        [| Pair.Of(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN)
           Pair.Of(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK)
           Pair.Of(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE)
           Pair.Of(FSharpTokenType.LESS, FSharpTokenType.GREATER) |]

    new () =
        FSharpSkipPairBracketsMatcher(skipPairBrackets)


let atChars = '@', '@'
let barChars = '|', '|'
let angledBracketsChars = '<', '>'

let isBacktick (lexer: ILexer) =
    lexer.TokenType == FSharpTokenType.RESERVED_SYMBOLIC_SEQUENCE && lexer.GetTokenText() = "`"

let tokenIs delta (predicate: ILexer -> bool) (lexer: CachingLexer) =
    use cookie = LexerStateCookie.Create(lexer)
    lexer.Advance(delta)
    isNotNull lexer.TokenType && predicate lexer

let nextTokenIs predicate lexer =
    tokenIs 1 predicate lexer

let prevTokenIs predicate lexer =
    tokenIs -1 predicate lexer


let tokenAtOffsetIs offset (predicate: ILexer -> bool) (lexer: CachingLexer) =
    if not (lexer.FindTokenAt(offset)) then false else
    predicate lexer


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
