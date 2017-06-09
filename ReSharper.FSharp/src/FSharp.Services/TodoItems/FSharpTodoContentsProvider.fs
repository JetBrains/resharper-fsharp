namespace JetBrains.ReSharper.Plugins.FSharp.Services.TodoItems

open System
open JetBrains.ReSharper.Feature.Services.TodoItems
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpTodoContentsProvider() =
    inherit DefaultTodoContentsProvider()
    
    override x.GetTokenContentsRange(text,range,tokenType) =
        match tokenType.IsComment with
        | true when text.RangeStartsWith(range, "///", StringComparison.Ordinal) -> Nullable(range.TrimLeft(3))
        | true when text.RangeStartsWith(range, "//", StringComparison.Ordinal) -> Nullable(range.TrimLeft(2))
        | true when text.RangeStartsWith(range, "/*", StringComparison.Ordinal) ->
            if text.RangeEndsWith(range, "*/", StringComparison.Ordinal) then Nullable(range.TrimLeft(2).TrimRight(2))
            else Nullable(range.Left(2))
        | _ -> base.GetTokenContentsRange(text,range,tokenType)
