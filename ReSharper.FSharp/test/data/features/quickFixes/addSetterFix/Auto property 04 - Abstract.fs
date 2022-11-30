[<AbstractClass>]
type T() =
    abstract member P: int
    member x.M() = x.P{caret} <- 23
