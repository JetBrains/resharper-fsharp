module Module

type T() =
    do
        let foo = 123  
        foo{caret} |> ignore
