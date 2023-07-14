module A

type E =
    | A = 1
    | B = 2

[<Literal>]
val c : E = E.B
