module Module

type I =
    abstract P2: int
    abstract M: int -> unit
    abstract P1: int

type T() =
    interface I{caret}
