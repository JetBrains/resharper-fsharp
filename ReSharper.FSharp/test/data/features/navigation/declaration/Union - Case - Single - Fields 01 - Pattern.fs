type U =
    | A of int

match A 123 with
| A{on} 123 -> ()
