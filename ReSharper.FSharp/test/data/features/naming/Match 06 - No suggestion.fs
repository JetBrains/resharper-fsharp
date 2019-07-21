module Module

type U = A of int
let foo = A 123

match foo with
| A x{caret} -> ()
| _ -> ()
