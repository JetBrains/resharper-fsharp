module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int

type B() =
    inherit A()

    override val P = 123
