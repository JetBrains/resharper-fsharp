[<AbstractClass>]
type A() =
    abstract P: int with get, set

[<AbstractClass>]
type B() =
    inherit A()

    override x.P = 1

type {caret}C() =
    inherit B()
