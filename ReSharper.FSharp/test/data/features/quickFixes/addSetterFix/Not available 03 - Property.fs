type T() =
    member x.P with get () = 42
    member x.M() = x.P{caret} <- 23
