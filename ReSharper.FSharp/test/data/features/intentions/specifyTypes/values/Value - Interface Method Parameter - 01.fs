module Members
type Q =
    abstract S: int -> unit

    type W() =
        interface Q with
            member this.S _{caret} = ()