module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default x.P = 1

type B() =
    inherit A()

    override x.P{on} = 123
