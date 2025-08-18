module Module

type T() =
    static member M(a, b) = a + b |> ignore

T.M(1, ""{caret})
