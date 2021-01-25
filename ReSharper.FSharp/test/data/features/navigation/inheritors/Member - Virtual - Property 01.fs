module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default x.P{on} = 1

type B() =
    inherit A()

    override x.P = 123
