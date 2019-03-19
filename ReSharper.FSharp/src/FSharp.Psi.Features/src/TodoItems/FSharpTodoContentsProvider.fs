namespace JetBrains.ReSharper.Plugins.FSharp.Services.TodoItems

open System
open JetBrains.ReSharper.Feature.Services.TodoItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpTodoContentsProvider() =
    inherit DefaultTodoContentsProvider()

    override x.GetTokenContentsRange(text, range, tokenType) =
        if not tokenType.IsComment then Nullable() else

        if text.RangeStartsWith(range, "///", StringComparison.Ordinal) then Nullable(range.TrimLeft(3)) else
        if text.RangeStartsWith(range, "//", StringComparison.Ordinal) then Nullable(range.TrimLeft(2)) else
        if text.RangeStartsWith(range, "(*", StringComparison.Ordinal) then
            if text.RangeEndsWith(range, "*)", StringComparison.Ordinal) then Nullable(range.TrimLeft(2).TrimRight(2))
            else Nullable(range.Left(2))

        else Nullable()
