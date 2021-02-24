module Module

let f (_: (int * int)) = ()
let f (_: (int * int) -> int) = ()

match [] with
| :? (int * int) -> ()
| :? (int * int) -> int -> ()
