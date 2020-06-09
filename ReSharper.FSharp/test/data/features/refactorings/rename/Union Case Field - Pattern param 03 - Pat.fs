module Module

type U =
    | C of f: int * g: int

match C(1, 2) with
| C (f = f; g = {caret}g) -> g |> ignore
| _ -> ()
