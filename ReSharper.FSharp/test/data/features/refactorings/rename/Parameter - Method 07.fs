module Module

type T() =
    static M([<obj>] a{caret}) = a |> ignore

T.M(a = 1)
