type T() =
    member this.M(a: int, b: int, c: int) = ()

let t = T()
t.M(q = 1{caret})
