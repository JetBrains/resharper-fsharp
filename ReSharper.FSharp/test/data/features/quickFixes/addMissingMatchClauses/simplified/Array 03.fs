module Say

type U =
    | A
    | B of string[]

match A{caret} with
| A -> ()
