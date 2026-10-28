type T() =
    member this.M (a: int, b: int) (c: int, d: int) = ()

let t = T()
t.M (1, 2) (d = 3, c = 4{caret})
