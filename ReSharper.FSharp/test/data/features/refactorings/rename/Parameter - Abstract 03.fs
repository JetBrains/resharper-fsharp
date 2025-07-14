module Module

type I =
    abstract M: a{caret}: int -> unit

type T() =
   interface I with
       member this.M(a) = a |> ignore

let i: I = Unchecked.defaultof<_>
i.M(a = 1)

