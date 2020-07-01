module Module

let add (a: int) (b: int) = a + b
let multiply (a: int) (b: int) = a * b
multiply 1 add {caret}add 2 3 4
