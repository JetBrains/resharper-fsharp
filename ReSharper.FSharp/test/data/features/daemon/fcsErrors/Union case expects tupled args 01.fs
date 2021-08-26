module Module

type U =
    | A
    | B of int * int

match A with
| A -> ()
| B a -> ()
