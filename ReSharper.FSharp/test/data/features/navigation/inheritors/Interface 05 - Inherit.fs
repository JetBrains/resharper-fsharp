module Module

type IA =
    abstract P{on}: int

type IB =
    inherit IA

type T() =
    interface IB with
        member x.P = 1
