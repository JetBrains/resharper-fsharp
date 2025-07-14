module Module

type T() =
    static M(a{caret}: int, b: int) = a |> ignore

T.M(a = 1, b = 2)
