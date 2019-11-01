namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ExtendSelection

open System
open JetBrains.Application.Settings
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
type FSharpSelectEmbracingConstructProvider(settingsStore: ISettingsStore) =
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
        let parent = x.TreeNode.Parent
        if isNull parent then null else
        FSharpTreeNodeSelection(fsFile, parent) :> _

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND


and FSharpTreeRangeSelection(fsFile, first, last) =
    inherit TreeRangeSelection<IFSharpFile>(fsFile, first, last)

    override x.Parent = null


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
