module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int

type B() =
    inherit A()

    member val P = 123
