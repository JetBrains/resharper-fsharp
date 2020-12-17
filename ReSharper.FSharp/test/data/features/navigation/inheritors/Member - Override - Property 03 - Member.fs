module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int

type B() =
    inherit A()

    member x.P = 123
