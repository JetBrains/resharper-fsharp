module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int

type B() =
    inherit A()

    default x.P = 123
