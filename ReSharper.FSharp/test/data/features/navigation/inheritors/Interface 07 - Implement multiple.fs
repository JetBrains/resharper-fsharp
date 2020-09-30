module Module

type IA =
    abstract P: int

type IB =
    inherit IA
    abstract P{on}: int

type T() =
    interface IB with
        member x.P = 1
