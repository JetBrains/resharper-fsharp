type U =
    | A of int * int

let _, a{caret} = (), A (1, 2)
let b = a
