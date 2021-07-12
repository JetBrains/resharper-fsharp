module Module

type U =
    | A
    | B of field: int

match A with
| B _{caret} -> ()
