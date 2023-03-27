type U =
    | A
    | B of int * int

match A with
| B b{caret} -> ignore b
