module Module

type Q =
        abstract S: int -> unit

    type W() =
        interface Q with
            member this.S{on} _ = ()