type R() =
    member val X = 0 with get, set

type T() =
    member this.M(a: int) = R()
    member this.M(a: int, b: int) = 0

let t = T()
t.M(X{caret})
