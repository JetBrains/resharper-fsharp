module Say

type U =
    | A
    | B of int * int * named: int

match A{caret} with
| A -> ()
