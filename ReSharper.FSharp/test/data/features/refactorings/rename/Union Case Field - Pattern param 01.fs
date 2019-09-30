module Module

type U =
    | C of f: int

match C 1 with
| C (f{caret} = f) -> ()
| _ -> ()
