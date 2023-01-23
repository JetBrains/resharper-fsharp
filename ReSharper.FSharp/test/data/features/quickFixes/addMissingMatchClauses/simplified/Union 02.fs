module Say

type U =
    | B of bool
    | A

match A{caret} with
| B true -> ()
| B false -> ()
