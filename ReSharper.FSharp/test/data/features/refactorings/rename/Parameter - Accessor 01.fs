module Module

type T() =
    static member P
        with get a =
            a + 1 |> ignore

T.P(a{caret} = 1)
