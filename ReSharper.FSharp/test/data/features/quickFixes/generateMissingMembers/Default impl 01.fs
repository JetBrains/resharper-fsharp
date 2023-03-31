[<AbstractClass>]
type A() =
    abstract P1: int

    abstract P2: int
    default this.P2 = 1

    abstract M1: int -> unit

    abstract M2: int -> unit
    default this.M2(i) = ()

type B{caret}() =
    inherit A()
