module Module

type T() =
    member _.M(a: int, [<System.Obsolete>] b: int) = ()

let t = T()
t.M(1, ""{caret})
