open System.Text.RegularExpressions

Regex.IsMatch("hello", @"^ab(group)
                         # comment
                         \d+$") |> ignore
