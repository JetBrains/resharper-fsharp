module Module

[<AbstractClass>]
type A() =
    abstract M{caret}: int -> unit

type T() =
    inherit A()
