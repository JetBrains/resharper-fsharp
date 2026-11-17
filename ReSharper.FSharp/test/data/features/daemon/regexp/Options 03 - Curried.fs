open System.Diagnostics.CodeAnalysis
open System.Text.RegularExpressions

let f ([<StringSyntax(StringSyntaxAttribute.Regex)>] pattern: string) (options: RegexOptions) = ()

f "[123]
   #comment
   " RegexOptions.IgnorePatternWhitespace

RegexOptions.IgnorePatternWhitespace |> f "[123]
                                           #comment
                                           "
