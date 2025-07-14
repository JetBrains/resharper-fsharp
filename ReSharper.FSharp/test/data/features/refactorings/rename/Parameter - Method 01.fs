module Module

type T() =
    static M(a{caret}) = a |> ignore

T.M(a = 1)
