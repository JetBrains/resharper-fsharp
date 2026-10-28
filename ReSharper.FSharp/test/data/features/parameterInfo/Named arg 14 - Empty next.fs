type T() =
    member this.M(a: int, b: int, c: int) = ()

let t = T()
t.M(1, b = 2, {caret})
