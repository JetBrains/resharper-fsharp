module Say

type U =
    | A
    | B of bool * bool * named: bool

match A{caret} with
| B true -> ()
