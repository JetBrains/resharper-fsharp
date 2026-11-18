module Module

[<AbstractClass>]
type A() =
    abstract P{caret}: int with get, set

type T() =
    inherit A()

    override this.P = 1
