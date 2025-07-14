module Module

type T() =
    abstract M: a: int * b: int -> c: int -> unit
    default this.M(a, b) c{caret} = a + b + c |> ignore

// Named args are supported for a single curried group only
