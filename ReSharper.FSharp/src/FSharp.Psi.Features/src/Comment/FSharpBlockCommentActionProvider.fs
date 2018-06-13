namespace JetBrains.ReSharper.Plugins.FSharp.Services.Comment

open JetBrains.ReSharper.Feature.Services.Comment
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.Util
open System.Runtime.InteropServices

[<Language(typeof<FSharpLanguage>)>]
type FSharpBlockCommentActionProvider() = 
    interface IBlockCommentActionProvider with
        
        member x.StartBlockCommentMarker = "(*"
        member x.EndBlockCommentMarker = "*)"
        member x.NestedStartBlockCommentMarker = null
        member x.NestedEndBlockCommentMarker = null

        member x.GetBlockComment(lexer) = 
            if lexer.TokenType == FSharpTokenType.COMMENT then TextRange(lexer.TokenStart, lexer.TokenEnd)
            else TextRange.InvalidRange

        member x.IsAvailable(file, range, [<Out>] disableAllProviders) =
            disableAllProviders <- false
            true

        member x.InsertBlockCommentPosition(lexer, position) = 
            let tokenType = lexer.TokenType
            if tokenType == FSharpTokenType.LINE_COMMENT then position else

            if tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.NEW_LINE ||
               tokenType == FSharpTokenType.COMMENT then lexer.TokenStart else

            if position = lexer.TokenStart then position else

            lexer.TokenEnd
