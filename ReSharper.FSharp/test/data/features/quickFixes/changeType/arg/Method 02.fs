module Module

[<AllowNullLiteral>]
type I =
    abstract M: named: int * int -> unit
    default this.M(a, b) = ()

let i: I = null
i.M(""{caret}, "")
