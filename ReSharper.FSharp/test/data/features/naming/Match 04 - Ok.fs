module Module

let foo = Ok ()

match foo with
| Ok x{caret} -> ()
| _ -> ()
