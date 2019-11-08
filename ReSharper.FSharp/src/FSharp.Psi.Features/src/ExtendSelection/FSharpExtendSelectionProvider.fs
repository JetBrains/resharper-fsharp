namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ExtendSelection

open System
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Editor
open JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
type FSharpExtendSelectionProvider(settingsStore: ISettingsStore) =
    static member ExtendNodeSelection(fsFile: IFSharpFile, node: ITreeNode): ISelectedRange =
        match node with
        | :? IFSharpIdentifier as identifier ->
            let decl = QualifiableModuleLikeDeclarationNavigator.GetByIdentifier(identifier)
            if isNotNull decl && isNotNull decl.QualifierReferenceName then
                FSharpTreeRangeSelection(fsFile, decl.QualifierReferenceName, identifier) :> _ else

            null

        | :? IReferenceName as referenceName ->
            let decl = QualifiableModuleLikeDeclarationNavigator.GetByQualifierReferenceName(referenceName)
            if isNotNull decl && isNotNull decl.Identifier then
                FSharpTreeRangeSelection(fsFile, referenceName, decl.Identifier) :> _ else

            null

        | :? ISynPat as synPat ->
            let matchClause = MatchClauseNavigator.GetByPattern(synPat)
            if isNotNull matchClause && isNotNull matchClause.WhenExpression then
                FSharpTreeRangeSelection(fsFile, synPat, matchClause.WhenExpression) :> _ else

            null

        | :? ISynExpr as expr ->
            let matchClause = MatchClauseNavigator.GetByWhenExpression(expr)
            if isNotNull matchClause && isNotNull matchClause.Pattern then
                FSharpTreeRangeSelection(fsFile, matchClause.Pattern, expr) :> _ else

            let binding = BindingNavigator.GetByExpression(expr)
            let letExpr = LetLikeExprNavigator.GetByBinding(binding)
            if isNotNull letExpr then
                FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letExpr, binding) else

            null

        | :? IBinding as binding ->
            let letBindings = LetNavigator.GetByBinding(binding)
            if isNotNull letBindings then
                FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letBindings, binding) else

            null

        | :? IAttributeList as attrList ->
            let letBindings = LetModuleDeclNavigator.GetByAttributeList(attrList)
            if isNotNull letBindings then
                FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letBindings) else

            null
        
        | :? ITokenNode as token ->
            match token.Parent with
            | :? ILet as letExpr ->
                let bindings = letExpr.Bindings
                if bindings.IsEmpty then null else

                let binding = letExpr.Bindings.[0]
                if token.GetTreeStartOffset().Offset < binding.GetTreeStartOffset().Offset then
                    FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letExpr, binding) else

                if getTokenType token != FSharpTokenType.AND then null else

                let letExpr = letExpr.As<ILetBindings>().NotNull()
                let separatorIndex = letExpr.Separators.IndexOf(token)
                if separatorIndex = -1 then null else
                
                let bindingIndex = separatorIndex + 1
                if bindingIndex >= bindings.Count then null else

                FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letExpr, bindings.[bindingIndex])

            | :? IMatchClause as matchClause ->
                if matchClause.WhenKeyword == token && isNotNull matchClause.WhenExpression then
                    FSharpTreeRangeSelection(fsFile, matchClause.Pattern, matchClause.WhenExpression) :> _ else

                null

            | _ -> null
        | _ -> null

    static member FindBetterNode(fsFile, node: ITreeNode) =
        let shouldTryFindBetterNode (node: ITreeNode) =
            node :? IBinding

        if not (shouldTryFindBetterNode node) then null else
        FSharpExtendSelectionProvider.ExtendNodeSelection(fsFile, node)

    static member CreateLetBindingSelection(fsFile, letBindings: ILet): ISelectedRange =
        let bindings = letBindings.Bindings
        if bindings.IsEmpty then null else
        FSharpExtendSelectionProvider.CreateLetBindingSelection(fsFile, letBindings, bindings.[0])

    static member CreateLetBindingSelection(fsFile, letExpr: ILet, binding): ISelectedRange =
        let bindings = letExpr.Bindings
        if bindings.[0] == binding then
            FSharpLetExprBindingSelection(fsFile, letExpr, letExpr.FirstChild, binding) :> _ else

        let letBindings = letExpr.As<ILetBindings>()
        if isNull letBindings then null else

        let index = bindings.IndexOf(binding) - 1
        let separators = letBindings.Separators
        if index = -1 || index >= separators.Count then null else

        FSharpLetExprBindingSelection(fsFile, letBindings, separators.[index], binding) :> _

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
    inherit DotSelection<IFSharpFile>(fsFile, offset, selectBetterToken, useCamelHumps)

    override x.IsWordToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsIdentifier || tokenType.IsKeyword

    override x.IsLiteralToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsConstantLiteral || tokenType.IsStringLiteral

    override x.IsSpaceToken(token) = token.GetTokenType().IsWhitespace
    override x.IsNewLineToken(token) = token.GetTokenType() == FSharpTokenType.NEW_LINE

    override x.CreateTokenPartSelection(node, range) =
        FSharpTokenPartSelection(fsFile, range, node) :> _

    override x.CreateTreeNodeSelection(tokenNode) =
        // todo: build more complex selection?
        FSharpTreeNodeSelection(fsFile, tokenNode) :> _

    override x.GetParentInternal(token) =
        let shouldCreateTokenPartSelection (tokenType: TokenNodeType) =
            tokenType.IsIdentifier || tokenType.IsComment ||
            tokenType.IsConstantLiteral || tokenType.IsStringLiteral

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
    inherit TreeRangeSelection<IFSharpFile>(fsFile, first, last)

    override x.Parent = FSharpTreeNodeSelection(fsFile, first.Parent) :> _

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND


and FSharpTokenPartSelection(fsFile, treeTextRange, token) =
    inherit TokenPartSelection<IFSharpFile>(fsFile, treeTextRange, token)

    override x.Parent =
        let tokenText = token.GetText()

        let trim left right =
            let range = token.GetTreeTextRange()
            if range.Length >= left + right then
                tokenText.Substring(left, tokenText.Length - left - right), left
            else tokenText, left

        let text, start =
            let tokenType = token.GetTokenType()
            if tokenType.IsStringLiteral then
                // todo: trim end if it actually ends with proper symbols?
                match tokenType.GetLiteralType() with
                | FSharpLiteralType.Character
                | FSharpLiteralType.RegularString -> trim 1 1
                | FSharpLiteralType.VerbatimString -> trim 2 1
                | FSharpLiteralType.TripleQuoteString -> trim 3 3
                | FSharpLiteralType.ByteArray -> trim 1 2

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
            let range = TokenPartSelection<_>.GetLocalParent(text, localRange)
            if range.IsValid() && range.Contains(&localRange) then
                let range = range.Shift(token.GetTreeStartOffset() + start)
                FSharpTokenPartSelection(fsFile, range, token) :> _
            else
                FSharpTreeNodeSelection(fsFile, token) :> _
        else
            FSharpTreeNodeSelection(fsFile, token) :> _


and FSharpLetExprBindingSelection(fsFile: IFSharpFile, letBindings: ILet, first: ITreeNode, last: ITreeNode) =
    inherit FSharpTreeRangeSelection(fsFile, first, last)

    override x.Parent =
        match letBindings with
        | :? ILetLikeExpr -> FSharpTreeRangeSelection(fsFile, letBindings.FirstChild, letBindings.Bindings.Last()) :> _
        | _ -> FSharpTreeNodeSelection(fsFile, letBindings) :> _
