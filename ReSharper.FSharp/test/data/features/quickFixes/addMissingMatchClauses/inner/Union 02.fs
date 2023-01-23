module Say

type U =
    | A
    | B of int

match A{caret} with
| A -> ()
