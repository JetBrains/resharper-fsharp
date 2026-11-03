type U =
    | A of b: bool * x: int

let a = 1
let u = A(a = 1, {caret})
