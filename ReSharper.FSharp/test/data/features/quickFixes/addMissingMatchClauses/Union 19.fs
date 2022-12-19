module Say

type U =
    | A
    | B of int * named: bool

match A{caret} with
| B(named = true) -> ()
