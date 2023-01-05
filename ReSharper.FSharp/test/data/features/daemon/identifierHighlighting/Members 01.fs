module Module

[<AbstractClass>]
type A() =
    abstract M: unit -> unit
    default this.M() = ()

type B() =
    inherit A()

    override this.M() =
        base.M()
