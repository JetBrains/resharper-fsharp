type U =
    | A of int * int

match A(1, 2) with
| _{caret} -> ()
