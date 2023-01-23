module Say

type U<'T> =
    | A
    | B of 'T

match B 1{caret} with
| A -> ()
