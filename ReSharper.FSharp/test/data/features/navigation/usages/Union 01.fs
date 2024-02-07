module Module

type U{on} =
    | A
    | B of int

match A with
| B _ -> ()
