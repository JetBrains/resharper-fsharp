module Say

type U =
    | A
    | B
    | C

do
    match A{caret} with
    A -> ()
