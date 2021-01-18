module Module

type I =
    abstract P1: int
    abstract P2: int

type T1() =
    interface I

type T2() =
    interface I with

type T3() =
    interface I with
        member x.P1 = 1
