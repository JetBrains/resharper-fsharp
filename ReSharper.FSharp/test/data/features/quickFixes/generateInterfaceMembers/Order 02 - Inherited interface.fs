module Module

type IA =
    abstract P3: int
    abstract P1: int

type IB =
    inherit IA

    abstract P4: int
    abstract P2: int

type T() =
    interface IB{caret}
