module Module

[<AbstractClass>]
type A() =
    abstract P{on}: int
    default val P = 1

type B() =
    inherit A()

    override val P = 123
