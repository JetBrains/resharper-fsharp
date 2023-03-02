module Say

let (|String|) x = string x

match true, true{caret} with
| true, String s -> ()
