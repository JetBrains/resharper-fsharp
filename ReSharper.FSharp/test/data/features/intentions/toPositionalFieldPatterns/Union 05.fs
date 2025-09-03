module Module

type U =
    | A
    | B of hello: int * foo: int option * world: string

match A with
| B({caret}hello = 1
    foo = Some
              1) -> ()
| _ -> ()
