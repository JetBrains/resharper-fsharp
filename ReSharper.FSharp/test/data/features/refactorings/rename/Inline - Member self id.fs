module Module

let x = 123

type T() as this =
    let x = 123

    member x.Foo =
        x |> ignore
        x{caret}
