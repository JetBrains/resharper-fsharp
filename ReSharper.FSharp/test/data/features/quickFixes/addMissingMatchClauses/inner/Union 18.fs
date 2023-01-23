module Say

type U =
    | A
    | B of bool * bool * bool * named: bool

match A{caret} with
| B(_, true) -> ()
