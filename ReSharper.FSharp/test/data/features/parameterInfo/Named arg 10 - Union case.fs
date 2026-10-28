type U =
    | A of x: int * y: int * z: int

let u = A(y = 1, x = 2{caret})
