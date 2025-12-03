type T() =
    member val P = 42 with get // commennt
    member x.M() = x.P{caret} <- 23
