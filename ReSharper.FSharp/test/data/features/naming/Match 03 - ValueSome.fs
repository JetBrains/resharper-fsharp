module Module

let foo = ValueSome ()

match foo with
| ValueSome x{caret} -> ()
| _ -> ()
