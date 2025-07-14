module Module

type T() =
    static member P
        with get {caret}a =
            a + 1 |> ignore

T.P(a = 1)
