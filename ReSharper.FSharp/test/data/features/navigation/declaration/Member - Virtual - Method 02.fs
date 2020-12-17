module Module

[<AbstractClass>]
type A() =
    abstract M: unit -> unit
    override x.M() = ()

A().M{on}()
