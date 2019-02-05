module Module

type T = A of int | B of int
let (A {caret}x | B x) = A 123
let y = x
