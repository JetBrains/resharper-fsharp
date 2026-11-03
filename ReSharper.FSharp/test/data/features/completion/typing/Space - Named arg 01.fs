// ${TEXT: }
module Module

type T() =
    member this.M(a: int, b: int, c: int) = ()

let t = T()
t.M(a = 1,{caret})
