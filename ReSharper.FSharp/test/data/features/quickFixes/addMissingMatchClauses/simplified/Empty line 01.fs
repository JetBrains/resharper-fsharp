module Say

type U =
    | A
    | B
    | C
    | D

do
    match A{caret} with
    | A -> ()

    | B -> ()
