module Module

let f a =
        fun (b, c{caret}) ->
    a + b + c |> ignore
