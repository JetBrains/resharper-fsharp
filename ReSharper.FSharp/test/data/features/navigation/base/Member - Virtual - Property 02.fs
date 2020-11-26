module Module

[<AbstractClass>]
type A() =
    abstract P: int
    override x.P = 1

type B() =
    inherit A()

    override x.P{on} = 123
