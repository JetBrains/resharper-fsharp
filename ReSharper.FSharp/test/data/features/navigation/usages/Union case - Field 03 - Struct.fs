namespace global

[<Struct>]
type U =
    | A of a{on}: int
    | B of b: int

module Module =
    match A(a = 123) with
    | A (a = 123) -> ()
