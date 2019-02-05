module Module

[<Struct>]
type Record =
    { Field: int }

    static member Dummy: Record = { Field = 123 }


let (r1: Record) = {caret}Record.Dummy
let (r2: Record) = Record.Dummy
