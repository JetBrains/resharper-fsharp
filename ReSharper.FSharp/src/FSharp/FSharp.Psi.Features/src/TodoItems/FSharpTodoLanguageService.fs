namespace JetBrains.ReSharper.Plugins.FSharp.Services.TodoItems

open System
open JetBrains.ReSharper.Feature.Services.TodoItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpTodoLanguageService() =
    inherit DefaultTodoLanguageService()

    let startsWith string range (text: string) =
        text.RangeStartsWith(range, string, StringComparison.Ordinal)

    let endsWith string range (text: string) =
        text.RangeEndsWith(range, string, StringComparison.Ordinal)

    override x.GetTokenContentsRange(text, range, tokenType) =
        if tokenType.IsComment then
            if startsWith "///" range text then Nullable(range.TrimLeft(3)) else
            if startsWith "//" range text then Nullable(range.TrimLeft(2)) else
            if startsWith "(*" range text then
                if endsWith "*)" range text then
                    Nullable(range.TrimLeft(2).TrimRight(2))
                else
                    Nullable(range.Left(2))
            else
                Nullable()

        elif tokenType.IsIdentifier then
            if startsWith "``" range text && endsWith "``" range text then
                Nullable(range.TrimLeft(2).TrimRight(2))
            else
                Nullable(range)

        else
            Nullable()

    override this.IsMultiLineComment(_, _, tokenType) =
        tokenType == FSharpTokenType.BLOCK_COMMENT
