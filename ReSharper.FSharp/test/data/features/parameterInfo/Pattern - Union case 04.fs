type U =
    | A of int

match A 1 with
| A {caret} -> ()
