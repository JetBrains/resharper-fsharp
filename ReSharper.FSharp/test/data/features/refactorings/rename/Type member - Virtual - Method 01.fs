type A() =
    abstract M: unit -> unit
    default x.M() = ()

type B() =
    inherit A()

    override x.M() = ()

A().M()
B().M{caret}()
