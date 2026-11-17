module Test

open System.Diagnostics.CodeAnalysis

let f (x: string) ([<StringSyntax(StringSyntaxAttribute.Regex)>] y: string) = ()
let g ([<StringSyntax(StringSyntaxAttribute.Regex)>] x: string) = ()

"\d" |> f "\d"
f "\d" <| "\d"
("\d", "\d") ||> f
"\d" |> g
g <| "\d"
