module Module

[<return: Struct>]
let (|A|_|) x = ValueSome 1.0

match "" with
| A x{caret} -> ()
