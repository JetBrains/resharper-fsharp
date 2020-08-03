module Module

type IA =
    abstract P: int

type IB =
    inherit IA

type T() =
    interface IB with
        member x.P{on} = 1
