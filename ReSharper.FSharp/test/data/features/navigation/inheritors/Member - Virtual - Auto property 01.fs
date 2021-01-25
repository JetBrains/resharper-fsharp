module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default val P{on} = 1

type B() =
    inherit A()

    override val P = 123
