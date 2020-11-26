type A() =
    abstract P: int
    override x.P = 1

type B() =
    inherit A()

    override x.P = 1

A().P
B().P{caret}
