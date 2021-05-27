namespace JetBrains.ReSharper.Plugins.FSharp.Services.Comment

open JetBrains.ReSharper.Feature.Services.Comment
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpBlockCommentActionProvider() =
    interface IBlockCommentActionProvider with

        member x.StartBlockCommentMarker = "(*"
        member x.EndBlockCommentMarker = "*)"
        member x.NestedStartBlockCommentMarker = null
        member x.NestedEndBlockCommentMarker = null

        member x.GetBlockComment(token) =
            if token.GetTokenType() == FSharpTokenType.BLOCK_COMMENT then
                TextRange(token.GetDocumentStartOffset().Offset, token.GetDocumentEndOffset().Offset)
            else
                TextRange.InvalidRange

        member x.IsAvailable(_, _, disableAllProviders) =
            disableAllProviders <- false
            true

        member x.InsertBlockCommentPosition(token, position) =
            let tokenType = token.GetTokenType()
            if tokenType == FSharpTokenType.LINE_COMMENT then position else

            if tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.NEW_LINE ||
               tokenType == FSharpTokenType.BLOCK_COMMENT then token.GetDocumentStartOffset().Offset else

            if position = token.GetDocumentStartOffset().Offset then position else

            token.GetDocumentEndOffset().Offset
