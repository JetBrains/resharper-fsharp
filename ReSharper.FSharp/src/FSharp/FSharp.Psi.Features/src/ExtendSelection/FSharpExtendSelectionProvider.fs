namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ExtendSelection

open System
open JetBrains.Application.Settings
open JetBrains.ReSharper.Feature.Services.Editor
open JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpExtendSelectionProvider(settingsStore: ISettingsStore) =
    static member ExtendNodeSelection(fsFile: IFSharpFile, node: ITreeNode): ISelectedRange =
        match node with
        | :? IFSharpIdentifier as identifier ->
            let decl = QualifiableDeclarationNavigator.GetByIdentifier(identifier)
            if isNotNull decl && isNotNull decl.QualifierReferenceName then
                FSharpTreeRangeSelection(fsFile, decl.QualifierReferenceName, identifier) :> _ else

            null

        | :? ITypeReferenceName as referenceName ->
            let attribute = AttributeNavigator.GetByReferenceName(referenceName)
            if isNotNull attribute && isNotNull attribute.ArgExpression then
                FSharpTreeRangeSelection(fsFile, referenceName, attribute.ArgExpression) :> _ else

            let typeInherit = TypeInheritNavigator.GetByTypeName(referenceName)
            if isNotNull typeInherit && isNotNull typeInherit.CtorArg then
                FSharpTreeRangeSelection(fsFile, referenceName, typeInherit.CtorArg) :> _ else

            let decl = QualifiableDeclarationNavigator.GetByQualifierReferenceName(referenceName)
            if isNotNull decl && isNotNull decl.Identifier then
                FSharpTreeRangeSelection(fsFile, referenceName, decl.Identifier) :> _ else

            null

        | :? IReferenceName as referenceName ->
            let decl = QualifiableDeclarationNavigator.GetByQualifierReferenceName(referenceName)
            if isNotNull decl && isNotNull decl.Identifier then
                FSharpTreeRangeSelection(fsFile, referenceName, decl.Identifier) :> _ else

            null

        | :? IFSharpPattern as fsPattern ->
            let matchClause = MatchClauseNavigator.GetByPattern(fsPattern)
            if isNotNull matchClause && isNotNull matchClause.WhenExpressionClause then
                FSharpTreeRangeSelection(fsFile, fsPattern, matchClause.WhenExpressionClause) :> _ else

            null

        | :? IFSharpExpression as expr ->
            let attribute = AttributeNavigator.GetByExpression(expr)
            if isNotNull attribute && isNotNull attribute.ReferenceName && isNotNull attribute.ArgExpression then
                FSharpTreeRangeSelection(fsFile, attribute.ReferenceName, attribute.ArgExpression) :> _ else

            let typeInherit = TypeInheritNavigator.GetByCtorArgExpression(expr)
            if isNotNull typeInherit && isNotNull typeInherit.TypeName && isNotNull typeInherit.CtorArg then
                FSharpTreeRangeSelection(fsFile, typeInherit.TypeName, typeInherit.CtorArg) :> _ else

            let binding = BindingNavigator.GetByExpression(expr)
            let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
            if isNotNull letExpr then
                FSharpBindingSelection(fsFile, binding, letExpr) :> _ else

            let interpolationStringExpr = InterpolatedStringExprNavigator.GetByInsert(expr)
            if isNotNull interpolationStringExpr then
                let prevLiteral = getPrevSibling expr
                let nextLiteral = getNextSibling expr
                if isNotNull prevLiteral && isNotNull nextLiteral &&
                    isInterpolatedStringPartToken (prevLiteral.GetTokenType()) && isInterpolatedStringPartToken (nextLiteral.GetTokenType()) then
                    FSharpInterpolatedStringInsertSelectionWithTokens(fsFile, prevLiteral, nextLiteral, interpolationStringExpr) :> _
                else null
            else

            null

        | :? IWhenExprClause as whenClause ->
            let matchClause = MatchClauseNavigator.GetByWhenExpressionClause(whenClause)
            if isNotNull matchClause && isNotNull matchClause.Pattern then
                FSharpTreeRangeSelection(fsFile, matchClause.Pattern, whenClause) :> _ else

            null

        | :? IBinding as binding ->
            let letBindings = LetBindingsNavigator.GetByBinding(binding)
            if isNotNull letBindings then
                FSharpBindingSelection(fsFile, binding, letBindings) :> _ else

            null

        | node when (FSharpTokenType.InterpolatedStrings[node.GetTokenType()] && node.Parent :? IInterpolatedStringExpr) ->
            let parentExpr = node.Parent :?> IInterpolatedStringExpr
            if parentExpr.Literals.Count < 2 then null else
            FSharpInterpolatedStringExpressionSelection(fsFile, node.Parent :?> IInterpolatedStringExpr) :> _

        | _ -> null

    static member FindBetterNode(fsFile, node: ITreeNode) =
        let shouldTryFindBetterNode (node: ITreeNode) =
            node :? IBinding ||
            FSharpTokenType.InterpolatedStrings[node.GetTokenType()]

        if not (shouldTryFindBetterNode node) then null else
        FSharpExtendSelectionProvider.ExtendNodeSelection(fsFile, node)

    interface ISelectEmbracingConstructProvider with
        member x.IsAvailable(sourceFile) =
            sourceFile.LanguageType :? FSharpProjectFileType

        member x.GetSelectedRange(sourceFile, documentRange) =
            let fsFile = sourceFile.FSharpFile
            let translatedRange = fsFile.Translate(documentRange)
            if not (translatedRange.IsValid()) then null else

            let offset = translatedRange.StartOffset
            let selectBetterToken = translatedRange.Length = 0
            let useCamelHumps = EditorOptions.DoUseCamelHumps(settingsStore, fsFile)
            FSharpDotSelection(fsFile, offset, selectBetterToken, useCamelHumps) :> _


and FSharpDotSelection(fsFile, offset, selectBetterToken, useCamelHumps) =
    inherit DotSelection<IFSharpFile>(fsFile, offset, selectBetterToken, useCamelHumps, true)

    override x.IsWordToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsIdentifier || tokenType.IsKeyword || tokenType == FSharpTokenType.UNDERSCORE

    override x.IsLiteralToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsConstantLiteral || tokenType.IsStringLiteral

    override this.IsPrevTokenBetter(prevToken, tokenNode) =
        let tokenType = tokenNode.GetTokenType()
        let isIdentifierLikeToken = FSharpTokenType.Identifiers[tokenType]
        isIdentifierLikeToken && tokenType != FSharpTokenType.IDENTIFIER && not (this.IsSpaceToken(prevToken)) ||

        base.IsPrevTokenBetter(prevToken, tokenNode)

    override x.IsSpaceToken(token) = token.GetTokenType().IsWhitespace
    override x.IsNewLineToken(token) = token.GetTokenType() == FSharpTokenType.NEW_LINE

    override x.CreateTokenPartSelection(node, range) =
        FSharpTokenPartSelection(fsFile, range, node) :> _

    override x.CreateTreeNodeSelection(tokenNode) =
        match tokenNode.Parent with
        | :? IUnitExpr as unitExpr ->
            FSharpTreeNodeSelection(fsFile, unitExpr) :> _
        | _ ->
            FSharpTreeNodeSelection(fsFile, tokenNode) :> _

    override x.GetParentInternal(token) =
        let shouldCreateTokenPartSelection (tokenType: TokenNodeType) =
            tokenType.IsIdentifier ||
            tokenType.IsComment ||
            tokenType.IsConstantLiteral ||
            tokenType.IsStringLiteral ||
            FSharpTokenType.InterpolatedStrings[tokenType]

        if not (shouldCreateTokenPartSelection (token.GetTokenType())) then null else
        x.CreateTokenPartSelection(token, TreeTextRange(offset))


and FSharpTreeNodeSelection(fsFile, node: ITreeNode) =
    inherit TreeNodeSelection<IFSharpFile>(fsFile, node)

    override x.Parent =
        let extended = FSharpExtendSelectionProvider.ExtendNodeSelection(fsFile, node)
        if isNotNull extended then extended else

        let parent = x.TreeNode.Parent
        if isNull parent then null else

        let betterParent = FSharpExtendSelectionProvider.FindBetterNode(fsFile, parent)
        if isNotNull betterParent then betterParent else

        FSharpTreeNodeSelection(fsFile, parent) :> _

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND


and FSharpTreeRangeSelection(fsFile, first: ITreeNode, last: ITreeNode) =
    inherit FSharpTreeRangeOffsetSelection(fsFile, first, last,
        Func<ITreeNode, _>(fun node -> node.GetTreeStartOffset()),
        Func<ITreeNode, _>(fun node -> node.GetTreeEndOffset()))


and FSharpTreeRangeOffsetSelection(fsFile, first: ITreeNode, last: ITreeNode, firstOffsetFunc, lastOffsetFunc) =
    inherit TreeRangeSelection<IFSharpFile>(fsFile, first, last, firstOffsetFunc, lastOffsetFunc)

    override x.Parent =
        FSharpTreeNodeSelection(fsFile, first.Parent) :> _

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND


and FSharpTokenPartSelection(fsFile, treeTextRange, token) =
    inherit TokenPartSelection<IFSharpFile>(fsFile, treeTextRange, token)

    let findInterpolationInsertRange (token: ITokenNode): ISelectedRange =
        let tokenType = token.GetTokenType()
        if not FSharpTokenType.InterpolatedStrings[tokenType] then null else

        let tokenTextEnd = TreeTextRange(token.GetTreeEndOffset()).ExtendLeft(getStringEndingQuotesLength token)
        let tokenTextStart = TreeTextRange(token.GetTreeStartOffset()).ExtendRight(getStringStartingQuotesLength token)

        let interpolatedStringExpr = token.GetContainingNode<IInterpolatedStringExpr>()
        if isNull interpolatedStringExpr then null else

        let createInsertSelectionForNextLiteral (): ISelectedRange =
            interpolatedStringExpr.LiteralsEnumerable
            |> Seq.tryFind (fun literal ->
                let startOffset = literal.GetTreeStartOffset()
                startOffset.Offset >= treeTextRange.EndOffset.Offset)
            |> Option.map (fun literal ->
                FSharpInterpolatedStringInsertSelectionWithTokens(fsFile, token, literal, interpolatedStringExpr) :> ISelectedRange)
            |> Option.defaultValue null

        let createInsertSelectionForPrevLiteral (): ISelectedRange =
            interpolatedStringExpr.LiteralsEnumerable
            |> Seq.pairwise
            |> Seq.tryFind (fun (_, next) -> next == token)
            |> Option.map (fun (prev, _) ->
                FSharpInterpolatedStringInsertSelectionWithTokens(fsFile, prev, token, interpolatedStringExpr) :> ISelectedRange)
            |> Option.defaultValue null

        if treeTextRange.ContainedIn(&tokenTextEnd) &&
            (isInterpolatedStringStartToken tokenType || isInterpolatedStringMiddleToken tokenType) then
            createInsertSelectionForNextLiteral ()
        elif treeTextRange.ContainedIn(&tokenTextStart) &&
            (isInterpolatedStringMiddleToken tokenType || isInterpolatedStringEndToken tokenType) then
            createInsertSelectionForPrevLiteral ()
        else
            null

    override x.Parent =
        let tokenType = token.GetTokenType()
        let tokenText = token.GetText()

        let trim left right =
            let range = token.GetTreeTextRange()
            if range.Length >= left + right then
                tokenText.Substring(left, tokenText.Length - left - right), left
            else tokenText, left

        let text, start =
            if tokenType.IsStringLiteral || FSharpTokenType.InterpolatedStrings[tokenType] then
                // todo: trim end if it actually ends with proper symbols?
                let left = getStringStartingQuotesLength token
                let right = getStringEndingQuotesLength token
                trim left right

            elif tokenType == FSharpTokenType.IDENTIFIER then
                if tokenText.Length > 4 &&
                        tokenText.StartsWith("``", StringComparison.Ordinal) &&
                        tokenText.EndsWith("``", StringComparison.Ordinal) then
                    trim 2 2
                else tokenText, 0

            elif tokenType == FSharpTokenType.LINE_COMMENT then
                let left = if tokenText.StartsWith("///") then 3 else 2
                trim left 0

            elif tokenType == FSharpTokenType.BLOCK_COMMENT then
                let right = if tokenText.EndsWith("*)") then 2 else 0
                trim 2 right

            else tokenText, 0

        if treeTextRange.IsValid() then
            let localRange = treeTextRange.Shift(-token.GetTreeStartOffset().Offset - start)
            let localParentRange = TokenPartSelection<_>.GetLocalParent(StringSlice(text), localRange)

            if isInterpolatedStringPartToken tokenType &&
                tokenText.Length <> 0 && text.Length = localParentRange.Length &&
                token.Parent :? IInterpolatedStringExpr then
                let parentExpr = token.Parent :?> IInterpolatedStringExpr
                if parentExpr.Literals.Count >= 2 then
                    FSharpInterpolatedStringExpressionSelection(fsFile, parentExpr) :> _ else
                FSharpTreeNodeSelection(fsFile, token) :> _
            else

            let interpolationInsertRange = findInterpolationInsertRange token
            if isNotNull interpolationInsertRange then
                interpolationInsertRange else

            if localParentRange.IsValid() && localParentRange.Contains(localRange) then
                let range = localParentRange.Shift(token.GetTreeStartOffset() + start)
                FSharpTokenPartSelection(fsFile, range, token) :> _ else

            let betterSelection = FSharpExtendSelectionProvider.FindBetterNode(fsFile, token)
            if isNotNull betterSelection then betterSelection else

            FSharpTreeNodeSelection(fsFile, token) :> _
        else
            FSharpTreeNodeSelection(fsFile, token) :> _

and FSharpInterpolatedStringInsertSelectionWithTokens(fsFile, first: ITreeNode, last: ITreeNode, expr: IInterpolatedStringExpr) =
    inherit FSharpTreeRangeOffsetSelection(fsFile, first, last,
        Func<_,_>(FSharpInterpolatedStringInsertSelectionWithTokens.FirstOffsetFunc),
        Func<_,_>(FSharpInterpolatedStringInsertSelectionWithTokens.LastOffsetFunc))

    override x.Parent =
        FSharpInterpolatedStringExpressionSelection(fsFile, expr) :> _

    static member LastOffsetFunc(node: ITreeNode) =
        seq {
            yield node :?> ITokenNode
            yield! TreeNodeExtensions.NextTokens node
        }
        |> Seq.tryFind (isWhitespaceOrComment >> not)
        |> Option.map (fun tokenNode ->
            let tokenType = tokenNode.GetTokenType()
            if isInterpolatedStringMiddleToken tokenType || isInterpolatedStringEndToken tokenType then
                tokenNode.GetTreeStartOffset().Shift(1)
            else
                node.GetTreeStartOffset())
        |> Option.defaultValue (node.GetTreeStartOffset())

    static member FirstOffsetFunc(node: ITreeNode) =
        seq {
            yield node :?> ITokenNode
            yield! TreeNodeExtensions.PrevTokens node
        }
        |> Seq.tryFind (isWhitespaceOrComment >> not)
        |> Option.map (fun tokenNode ->
            let tokenType = tokenNode.GetTokenType()
            if isInterpolatedStringStartToken tokenType || isInterpolatedStringMiddleToken tokenType then
                tokenNode.GetTreeEndOffset().Shift(-1)
            else
                node.GetTreeEndOffset())
        |> Option.defaultValue (node.GetTreeEndOffset())

and FSharpInterpolatedStringExpressionSelection(fsFile: IFSharpFile, expr: IInterpolatedStringExpr) =
    inherit FSharpTreeRangeOffsetSelection(fsFile, expr.Literals.First(), expr.Literals.Last(),
        Func<_,_>(FSharpInterpolatedStringExpressionSelection.FirstOffsetFunc),
        Func<_,_>(FSharpInterpolatedStringExpressionSelection.LastOffsetFunc))

    static member LastOffsetFunc(node: ITreeNode) =
        let token = node.As<ITokenNode>()
        let quotesLength = if isNotNull token then getStringEndingQuotesLength token else 0
        node.GetTreeEndOffset().Shift(-quotesLength)

    static member FirstOffsetFunc(node: ITreeNode) =
        let token = node.As<ITokenNode>()
        let quotesLength = if isNotNull token then getStringStartingQuotesLength token else 0
        node.GetTreeStartOffset().Shift(quotesLength)

and FSharpBindingSelection(fsFile: IFSharpFile, binding: IBinding, letBindings: ILetBindings) =
    inherit FSharpTreeRangeSelection(fsFile, binding, binding)

    override x.Parent =
        match letBindings with
        | :? ILetOrUseExpr -> FSharpTreeRangeSelection(fsFile, letBindings.FirstChild, letBindings.Bindings.Last()) :> _
        | _ -> FSharpTreeNodeSelection(fsFile, letBindings) :> _
