module Module

type IA =
    abstract P: int

type IB =
    inherit IA
    abstract P: int

type T() =
    interface IB with
        member x.P{on} = 1
