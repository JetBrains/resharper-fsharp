module Say

type U =
    | A
    | B of named: int

let t = A, A
match t{caret} with
| A, A -> ()
