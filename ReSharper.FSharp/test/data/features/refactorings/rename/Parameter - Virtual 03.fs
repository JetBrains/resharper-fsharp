module Module

type T() =
    abstract M: a: int * b: int -> unit
    default this.M(a, b{caret}) = a + b |> ignore

let t = T()
t.M(a = 1, b = 2)
