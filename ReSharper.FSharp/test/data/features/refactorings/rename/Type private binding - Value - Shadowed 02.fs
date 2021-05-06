module Module

type T() =
    let v = 1
    let v = string v

    member x.P =
        v{caret} + ""
