module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int
    default x.P = 1

type B() =
    inherit A()

    override x.P = 123
