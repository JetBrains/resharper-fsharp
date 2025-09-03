module Module

type T() =
    static M((a{caret}: int)) = a |> ignore

T.M(a = 1)
