[<AbstractClass>]
type A<'T>() =
    abstract M1: unit -> 'T list
    abstract M2: unit -> int

type {caret}B() =
    inherit A<int>()

    override this.M1() = []
