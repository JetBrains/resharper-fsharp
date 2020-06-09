module Module

type U =
    | C of f: int * g{caret}: int

match C(1, 2) with
| C (f = f; g = g) -> ()
| _ -> ()
