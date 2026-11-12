module Module

type IA =
    abstract P2: int
    abstract P1: int

type IB =
    inherit IA
    abstract P4: int

type IC =
    inherit IA
    abstract P3: int

type ID =
    inherit IB
    inherit IC
    abstract P5: int

type T() =
    interface ID{caret}
