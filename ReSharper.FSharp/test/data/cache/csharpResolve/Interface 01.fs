namespace global

type T1 =
    abstract P: int

type T2 =
    inherit System.IDisposable

type T3 =
    abstract P1: int
    member this.P2 = 1

type T4() =
    abstract P: int
