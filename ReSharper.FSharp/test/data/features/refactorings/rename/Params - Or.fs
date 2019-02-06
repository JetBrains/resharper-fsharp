module Module

type T =
    | A of int
    | B of int

let foo (A x | B x) =
    x + x{caret}
