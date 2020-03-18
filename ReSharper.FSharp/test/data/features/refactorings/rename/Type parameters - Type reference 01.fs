module Module

type T<'TP>() =
    do
        typeof<'TP{caret}> |> ignore
