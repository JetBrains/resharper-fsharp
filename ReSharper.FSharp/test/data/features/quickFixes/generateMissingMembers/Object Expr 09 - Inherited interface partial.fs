module Module

type IA =
    abstract M1: unit -> unit

type IB =
    inherit IA

    abstract M2: unit -> unit

let o = { new IB{caret} with
            member this.M1() = () }
