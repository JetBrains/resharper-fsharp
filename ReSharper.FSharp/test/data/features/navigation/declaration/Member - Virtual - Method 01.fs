module Module

[<AbstractClass>]
type A() =
    abstract M: unit -> unit
    default x.M() = ()

A().M{on}()
