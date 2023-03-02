module Say

let (|Id|) x = x

match true{caret} with
| Id true -> ()
