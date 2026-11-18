module Module

[<AbstractClass>]
type A() =
    abstract P1: int

type B() =
    inherit A()

    override _.P1 = 1

type C{caret}() =
    inherit B()

    abstract P2: int
