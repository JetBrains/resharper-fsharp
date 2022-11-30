type T() =
    static member val P = 42
    member x.M() = T.P{caret} <- 23
