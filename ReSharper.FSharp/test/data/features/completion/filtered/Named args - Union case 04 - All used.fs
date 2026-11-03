type U =
    | A of x: int * y: int

let u = A(x = 1, y = 1, {caret})
