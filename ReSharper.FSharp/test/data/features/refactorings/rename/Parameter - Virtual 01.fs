module Module

type T() =
    abstract M: a: int -> unit
    default this.M(a{caret}) = a |> ignore

let t = T()
t.M(a = 1)
