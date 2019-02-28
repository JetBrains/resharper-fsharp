module Module

type R = { Name: string }
    with 
        member __.M _ = 
            sprintf "%A"
        override __.ToString() = 
            sprintf "%d" 1
type DU = DU
    with
        member __.M = 
            sprintf "%A"
        override __.ToString() = 
            sprintf "%d" 1
