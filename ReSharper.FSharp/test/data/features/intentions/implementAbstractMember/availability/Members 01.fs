module Module

type I =
    abstract{off} M{on}: int{off} -> unit
    abstract P{on}: int

[<AbstractClass>]
type A() =
    abstract M1{on}: int -> unit

    abstract M2{off}: int -> unit
    default this.M2{off}(i) = ()

    member this.M3{off}() = ()
