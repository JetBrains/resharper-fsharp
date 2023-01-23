module Say

type U =
    | A
    | B of ``type``: int

match A{caret} with
| A -> ()
