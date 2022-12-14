module Say

type U =
    | A
    | B of bool
    | C of bool * bool

match A{caret} with
| B true -> ()
| C(true, _) -> ()
