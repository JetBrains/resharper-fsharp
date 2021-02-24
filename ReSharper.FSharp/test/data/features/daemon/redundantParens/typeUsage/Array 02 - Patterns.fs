module Module

let f (_: (int[])) = ()
let f (_: (int[]) as a) = ()

match [] with
| :? (obj[]) -> ()
| :? (obj[]) as a -> ()
| :? (obj[]) as a as b -> ()
