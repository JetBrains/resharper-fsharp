module Say

type U =
    | A
    | B of ``type``: int

let t = A, A

match t{caret} with
| A, A -> ()
