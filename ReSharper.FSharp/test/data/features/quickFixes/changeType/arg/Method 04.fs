module Module

type T() =
    member _.M(a, b) = a + b |> ignore

let t = T()
t.M(1, ""{caret})
