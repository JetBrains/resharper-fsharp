module Module

type IA<'T> =
    abstract P2: 'T with get, set
    abstract P1: 'T

type IB =
    inherit IA<int>
    inherit IA<string>

type T() =
    interface IB{caret}
