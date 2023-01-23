module Say

type U =
    | A
    | B of bool

match A{caret} with
| B _ -> ()
