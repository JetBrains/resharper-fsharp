module Module

[<AbstractClass>]
type A() =
    abstract P: int
    default x.P = 1

A().P{on}
