module Module

type U =
    | A
    | B of int * hello: int option * world: string

let i = 1

match A with
| B({caret}hello = Some 1) -> ignore i
| _ -> ()
