module Module

let add (a: int) (b: int) = a + b
let add2 = add 2
let multiply (a: int) (b: int) = a * b
multiply 1 add2 {caret}3
