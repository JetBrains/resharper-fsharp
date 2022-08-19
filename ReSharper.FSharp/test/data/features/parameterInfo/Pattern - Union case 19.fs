type U =
    | A of int * string
    | B

match B with
| A (1, {caret}x) -> ()
