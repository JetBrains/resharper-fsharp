type A() =
    abstract M: unit -> unit
    override x.M() = ()

type B() =
    inherit A()

    override x.M() = ()

A().M()
B().M{caret}()
