[<Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText

open System
open FSharp.Compiler.Text
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.UI.RichText

let layoutTagLookup =
    [
        TextTag.ActivePatternCase, FSharpHighlightingAttributeIds.ActivePatternCase
        TextTag.ActivePatternResult, FSharpHighlightingAttributeIds.ActivePatternCase
        TextTag.Alias, FSharpHighlightingAttributeIds.Class
        TextTag.Class, FSharpHighlightingAttributeIds.Class
        TextTag.Enum, FSharpHighlightingAttributeIds.Enum
        TextTag.Union, FSharpHighlightingAttributeIds.Union
        TextTag.UnionCase, FSharpHighlightingAttributeIds.UnionCase
        TextTag.Delegate, FSharpHighlightingAttributeIds.Delegate
        TextTag.Event, FSharpHighlightingAttributeIds.Event
        TextTag.Field, FSharpHighlightingAttributeIds.Field
        TextTag.Interface, FSharpHighlightingAttributeIds.Interface
        TextTag.Keyword, FSharpHighlightingAttributeIds.Keyword
        TextTag.Local, FSharpHighlightingAttributeIds.Value
        TextTag.Record, FSharpHighlightingAttributeIds.Record
        TextTag.RecordField, FSharpHighlightingAttributeIds.Field
        TextTag.Method, FSharpHighlightingAttributeIds.Method
        TextTag.Member, FSharpHighlightingAttributeIds.Property
        TextTag.ModuleBinding, FSharpHighlightingAttributeIds.Value
        TextTag.Module, FSharpHighlightingAttributeIds.Module
        TextTag.Namespace, FSharpHighlightingAttributeIds.Namespace
        TextTag.NumericLiteral, FSharpHighlightingAttributeIds.Number
        TextTag.Operator, FSharpHighlightingAttributeIds.Operator
        TextTag.Parameter, FSharpHighlightingAttributeIds.Value
        TextTag.Property, FSharpHighlightingAttributeIds.Property
        TextTag.StringLiteral, FSharpHighlightingAttributeIds.String
        TextTag.Struct, FSharpHighlightingAttributeIds.Struct
        TextTag.TypeParameter, FSharpHighlightingAttributeIds.TypeParameter
        TextTag.UnknownType, FSharpHighlightingAttributeIds.Class
        TextTag.UnknownEntity, FSharpHighlightingAttributeIds.Value
    ]
    |> List.map (fun (tag, attributeId) -> tag, TextStyle(attributeId))
    |> readOnlyDict

let toTextStyle (tag: TextTag) =
    match layoutTagLookup.TryGetValue(tag) with
    | true, style -> style
    | false, _ -> TextStyle.Default

let emptyPresentation = RichTextBlock()

[<Extension; CompiledName("ToRichText")>]
let richTextR (taggedText: TaggedText[]) =
    let result = RichText()

    let mutable isAfterNewLine = false
    for text in taggedText do
        match text.Tag with
        | TextTag.LineBreak -> isAfterNewLine <- true

        | TextTag.Space when isAfterNewLine ->
            // RIDER-51304: Replace spaces at the start of a line with \xA0 (non-breaking space)
            result.Append("\n" + String('\xA0', text.Text.Length), TextStyle.Default) |> ignore
            isAfterNewLine <- false

        | tag ->
            result.Append(text.Text, toTextStyle tag) |> ignore
            isAfterNewLine <- false

    result

let isEmpty (taggedText: TaggedText[]) =
    Array.isEmpty taggedText ||
    Array.length taggedText = 1 && taggedText.[0].Text = ""

let richTextJoin (sep : string) (parts : RichText seq) =
    let sep = RichText(sep, TextStyle.Default)
    parts |> Seq.fold (fun (result: RichText) part -> if result.IsEmpty then part else result + sep + part)
        RichText.Empty
