type U =
    | A of int
    | B

match B with
| A{on} 123 -> ()
