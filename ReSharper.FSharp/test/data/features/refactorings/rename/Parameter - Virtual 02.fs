module Module

type T() =
    abstract M: a{caret}: int -> unit
    default this.M(a) = a |> ignore

let t = T()
t.M(a = 1)
