open System.Text.RegularExpressions

let [<Literal>] options = RegexOptions.IgnorePatternWhitespace

Regex.IsMatch("hello", @"^ab(group)
                         # comment
                         \d+$", options) |> ignore
