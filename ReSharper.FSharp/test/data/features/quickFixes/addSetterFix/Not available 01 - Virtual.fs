type T() =
    abstract member P: int
    default x.P = 42
    member x.M() = x.P{caret} <- 23
