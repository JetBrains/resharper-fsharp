module Say

type U =
    | A
    | B of bool * bool

match A{caret} with
| B(_, true, true) -> ()
