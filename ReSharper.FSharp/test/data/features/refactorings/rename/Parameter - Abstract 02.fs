module Module

type I =
    abstract M: a: int -> unit

let i: I = Unchecked.defaultof<_>
i.M(a{caret} = 1)

