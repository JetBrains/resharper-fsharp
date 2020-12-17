module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default val P = 1

type B() =
    inherit A()

    override val P{on} = 123
