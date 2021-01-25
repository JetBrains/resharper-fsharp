module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default val P = 1

A().P{on}
