module Module

[<Struct>]
type S =
    val Field: int
    static member Dummy: S = Unchecked.defaultof<S>

let (s1: S) = {caret}S.Dummy
let (s2: S) = S.Dummy
