module A

type E =
    | A = 1
    | B = 2

[<Literal>]
let c{caret} = E.A
