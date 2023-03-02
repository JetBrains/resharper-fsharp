module Say

let (|Id|) x = x

match true, true{caret} with
| true, Id x -> ()
