module Say

let (|Pair|) x = x, x

match true, true{caret} with
| true, Pair _ -> ()
