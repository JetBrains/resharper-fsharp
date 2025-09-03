module Module

type I =
    abstract M: a{caret}: int -> unit

let i: I = Unchecked.defaultof<_>
i.M(a = 1)

