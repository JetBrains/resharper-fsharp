//${APP_OCCURRENCE:div}
//${ARGS_OCCURRENCE:div 2 3}
let add (a: int) (b: int) = a + b
let div (a: int) (b: int) = a / b
let multiply (a: int) (b: int) = a * b
multiply 1 add {caret}div 2 3 4
