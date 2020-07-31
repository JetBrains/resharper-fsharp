module Module

type IA =
    abstract P{on}: int

type IB =
    inherit IA
    abstract P: int

type T() =
    interface IB with
        member x.P = 1
