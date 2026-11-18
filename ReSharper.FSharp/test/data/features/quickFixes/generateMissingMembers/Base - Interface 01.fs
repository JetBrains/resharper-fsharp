module Module

type I =
    abstract M1: int -> unit

[<AbstractClass>]
type A() =
    abstract M2: int -> unit

type T{caret}() =
    inherit A()

    interface I
