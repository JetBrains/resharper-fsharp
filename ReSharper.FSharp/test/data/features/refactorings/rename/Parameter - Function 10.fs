module Module

let f a =
        fun b ->
            fun c{caret} ->
    a + b + c |> ignore
