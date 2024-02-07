module Module

type U =
    | A
    | B of hello: int * foo: int option * world: string

match A with
| B({caret}hello = Some 1) -> ()
| _ -> ()
