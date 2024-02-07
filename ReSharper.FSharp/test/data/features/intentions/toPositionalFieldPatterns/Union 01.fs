module Module

type U =
    | A
    | B of int * hello: int option * world: string

match A with
| B({caret}hello = Some 1) -> ()
| _ -> ()
