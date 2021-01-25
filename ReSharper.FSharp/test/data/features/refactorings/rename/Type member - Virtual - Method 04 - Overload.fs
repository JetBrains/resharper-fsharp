type A() =
    member this.M() = ()

    abstract M: int -> unit
    default x.M _ = ()

type B() =
    inherit A()

    override x.M _ = ()

A().M 1
B().M{caret} 1
