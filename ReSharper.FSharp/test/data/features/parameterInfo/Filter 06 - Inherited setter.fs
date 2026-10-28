type Base() =
    member val X = 0 with get, set

type Derived() =
    inherit Base()

type T() =
    member this.M(a: int) = Derived()
    member this.M(a: int, b: int) = 0

let t = T()
t.M(1, X = {caret})
