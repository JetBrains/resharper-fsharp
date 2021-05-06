module Module

type T() =
    let v = 1

    member x.P =
        v + v{caret} + 1
