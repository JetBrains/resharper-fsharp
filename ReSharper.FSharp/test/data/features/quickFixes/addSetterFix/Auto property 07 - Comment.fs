type T() =
    member val P = 42 // commennt
    member x.M() = x.P{caret} <- 23
