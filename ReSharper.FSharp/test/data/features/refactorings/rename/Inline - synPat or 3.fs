module Module

type T =
    | A of int
    | B of int

let x = 123

match A x with
| A x
| B x -> {caret}x + x
