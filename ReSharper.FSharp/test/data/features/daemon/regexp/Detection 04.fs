module Test

open System.Diagnostics.CodeAnalysis
open System.ComponentModel.DataAnnotations

type RegexAttr([<StringSyntax(StringSyntaxAttribute.Regex)>] pattern: string) =
    inherit System.Attribute()

    new(_: int) = RegexAttr("\d")
    new([<StringSyntax(StringSyntaxAttribute.Regex)>] pattern: string, _: int) = RegexAttr(pattern)

    [<StringSyntax(StringSyntaxAttribute.Regex)>]
    member val Pattern = "" with get, set


[<RegularExpression("\d", ErrorMessage = "")>]
let _ = ""

[<RegexAttr("\d", Pattern = "\d")>]
let _ = ""

[<RegexAttr(1, Pattern = "\d")>]
let _ = ""

[<RegexAttr("\d", 1, Pattern = "\d")>]
let _ = ""
