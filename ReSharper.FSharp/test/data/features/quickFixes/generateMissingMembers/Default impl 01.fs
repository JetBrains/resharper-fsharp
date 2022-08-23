[<AbstractClass>]
type A() =
    abstract P1: int
    default this.P1 = 1

    abstract P2: int

type B{caret}() =
    inherit A()
