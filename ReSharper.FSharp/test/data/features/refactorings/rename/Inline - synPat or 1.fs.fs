module Module

type T =
    | A of int
    | B of int

let x = 123

match A x with
| A {caret}x
| B x -> x + 1
