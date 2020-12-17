module Module

[<AbstractClass>]
type A() =
    abstract M: unit -> unit
    override x.M() = ()

type B() =
    inherit A()

    override x.M{on}() = ()
