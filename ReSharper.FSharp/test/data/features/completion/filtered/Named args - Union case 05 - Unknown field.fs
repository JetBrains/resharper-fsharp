// ${FILTER_ITEMS:a,x,y}
type U =
    | A of x: int * y: int

let a = 1
let u = A(a = 1, {caret})
