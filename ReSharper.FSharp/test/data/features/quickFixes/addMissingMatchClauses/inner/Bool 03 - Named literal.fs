module Say

let [<Literal>] True = true

match true{caret} with
| True -> ()
