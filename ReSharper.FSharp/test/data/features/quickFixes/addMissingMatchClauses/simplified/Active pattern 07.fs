module Say

let (|Pair|) x = x, x

match true, true{caret} with
| false, Pair _ -> ()
