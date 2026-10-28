type T() =
    member this.M(a: int, b: int, c: int) = ()

let t = T()
t.M(b = 1, a = {caret})
