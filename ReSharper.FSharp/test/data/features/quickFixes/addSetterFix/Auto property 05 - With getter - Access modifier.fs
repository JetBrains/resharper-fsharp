type T() =
    member val P = 42 with private get
    member x.M() = x.P{caret} <- 23
