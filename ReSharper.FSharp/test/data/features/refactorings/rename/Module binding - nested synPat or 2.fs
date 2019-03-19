module Module

type T = A of int | B of int
let (A x | B {caret}x) = A 123
let y = x
