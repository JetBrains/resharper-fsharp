module Module

[<AbstractClass>]
type A() =
    abstract P: int
    override val P = 1

type B() =
    inherit A()

    override val P{on} = 123
