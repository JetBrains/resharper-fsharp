[<AbstractClass>]
type A() =
    abstract P1: int
    abstract P2: int
    abstract P3: int

type {caret}B() =
    inherit A()

    override this.P1 = 1

type B with
    override this.P2 = 1
