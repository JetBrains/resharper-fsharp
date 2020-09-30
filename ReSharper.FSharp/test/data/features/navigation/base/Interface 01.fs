module Module

type IA =
    abstract P: int

type T() =
    interface IA with
        member x.P{on} = 1
