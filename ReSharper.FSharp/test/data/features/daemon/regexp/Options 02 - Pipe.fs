open System.Diagnostics.CodeAnalysis
open System.Text.RegularExpressions

let f (options: RegexOptions) ([<StringSyntax(StringSyntaxAttribute.Regex)>] pattern: string) = ()

"[123]
 #comment
 " |> f RegexOptions.None

"[123]
 #comment
 " |> f RegexOptions.IgnorePatternWhitespace

f RegexOptions.IgnorePatternWhitespace <| "[123]
                                          #comment
                                          "
