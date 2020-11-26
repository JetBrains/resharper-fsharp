[<AbstractClass>]
type A() =
    abstract M: unit -> unit
    member this.M(x: int) = ()

type {caret}B() =
    inherit A()
