[<AbstractClass>]
type A() =
    abstract P: int with get, set

type {caret}T() =
    inherit A()

    override x.P = 1
