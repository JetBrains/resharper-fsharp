module Module

[<AbstractClass>]
type A() =
    abstract P{caret}: int

type T() =
    inherit A()
