namespace global

type U =
    | A of a{on}: int

module Module =
    match A(a = 123) with
    | A (a = 123) -> ()
