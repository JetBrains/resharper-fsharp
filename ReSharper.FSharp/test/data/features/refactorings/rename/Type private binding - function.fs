module Module

type T() =
    let f x = 1

    member x.P =
        f 1 + f{caret} + 1
