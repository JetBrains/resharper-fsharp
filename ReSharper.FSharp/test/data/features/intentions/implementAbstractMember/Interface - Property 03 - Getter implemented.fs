module Module

type I =
    abstract P{caret}: int with get, set

type T() =
    interface I with
        member this.P = 1
