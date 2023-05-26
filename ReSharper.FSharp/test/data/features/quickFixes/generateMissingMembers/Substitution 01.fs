[<AbstractClass>]
type A<'T>() =
    abstract P1: 'T list
    abstract P2: int

type {caret}B() =
    inherit A<int>()

    override this.P1 = []
