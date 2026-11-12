module Module

type I =
    abstract P3: int
    abstract P1: int
    abstract P2: int

type T() =
    interface I{caret} with
        member this.P1 = 1
