type U =
    | A
    | B of U
    | C of int * string

match A with
| B (C ({caret}1, "")) -> ()
