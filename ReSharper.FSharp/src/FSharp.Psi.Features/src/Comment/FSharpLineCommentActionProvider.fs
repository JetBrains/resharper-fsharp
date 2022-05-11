namespace JetBrains.ReSharper.Plugins.FSharp.Services.Comment

open JetBrains.Application.Settings
open JetBrains.ReSharper.Feature.Services.Comment
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpLineCommentActionProvider() =
    inherit SimpleLineCommentActionProvider()

    override x.StartLineCommentMarker = "//"
    override x.IsNewLine(tokenType) = tokenType == FSharpTokenType.NEW_LINE
    override x.IsWhitespace(tokenType) = tokenType == FSharpTokenType.WHITESPACE

    override x.IsEndOfLineComment(tokenType, tokenText) =
        tokenType == FSharpTokenType.LINE_COMMENT &&
        (tokenText.Length = 2 || tokenText[2] <> '/' || tokenText.Length > 3 && tokenText[3] = '/')

    override x.ShouldInsertAtLineStart(settingsStore) =
        settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.PlaceCommentsAtFirstColumn) &&
        settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.StickComment)
