module Module

type T() =
    member x.M(a: int) = ()
    member x.P1 with set (a: int) (b: int) = ()
    member x.P2 with set ((a: int)) ((b: int)) = ()
