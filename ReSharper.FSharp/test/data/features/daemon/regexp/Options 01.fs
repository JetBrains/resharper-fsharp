open System.Text.RegularExpressions

let [<Literal>] options = RegexOptions.IgnorePatternWhitespace

let _ = Regex("[123]
              #comment
              ")

let _ = Regex("[123]
              #comment
              ", RegexOptions.IgnorePatternWhitespace)

let _ = Regex("[123]
              #comment
              ", options)
