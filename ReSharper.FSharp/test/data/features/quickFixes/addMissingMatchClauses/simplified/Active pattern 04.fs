module Say

let (|String|) x = string x

match true{caret} with
| String "" -> ()
