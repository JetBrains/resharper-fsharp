module Module

type T() =
    member _.M(a: int, b: int) = a + b |> ignore

let t = T()
t.M(1, ""{caret})
