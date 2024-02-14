module Module

type Base() =
    abstract P1: int
    default this.P1 = 1

    abstract P2: int

{ new {caret}Base() with
    override this.P1 = 1 }
