module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int

type B() =
    inherit A()

    default val P = 123
