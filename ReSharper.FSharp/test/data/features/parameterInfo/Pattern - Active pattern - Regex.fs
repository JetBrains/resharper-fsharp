open System.Text.RegularExpressions

let (|Regex|_|) pattern = Some pattern

match 1 with
| Regex {caret}"123" _ -> ()
