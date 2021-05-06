module Module

type T() =
    let v = 1
    let v = string v{caret}

    member x.P = v
