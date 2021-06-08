module Module

type T() =
    inherit Class()

    member x.P =
        fun _ ->
            x.ProtectedProp{caret}
        |> ignore
