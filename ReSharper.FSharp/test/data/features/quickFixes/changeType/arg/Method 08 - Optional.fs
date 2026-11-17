module Module

type T() =
    member _.M(a: int, ?b: int) = ()

let t = T()
t.M(1, ""{caret})
