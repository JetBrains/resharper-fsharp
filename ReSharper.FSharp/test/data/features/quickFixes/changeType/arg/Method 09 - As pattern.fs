module Module

type T() =
    member _.M(a: int, (b as c)) = b + c + 1 |> ignore

let t = T()
t.M(1, ""{caret})
