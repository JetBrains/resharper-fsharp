module Module

type T() =
    abstract M: a: int * b: int -> c{caret}: int -> unit
    default this.M(a, b) c = a + b + c |> ignore

// Named args are supported for a single curried group only
