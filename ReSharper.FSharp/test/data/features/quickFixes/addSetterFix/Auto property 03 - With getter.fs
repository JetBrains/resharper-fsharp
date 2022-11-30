type T() =
    member val P = 42 with get
    member x.M() = x.P{caret} <- 23
