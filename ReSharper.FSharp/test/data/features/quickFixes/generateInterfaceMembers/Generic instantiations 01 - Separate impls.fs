module Module

type IA<'T> =
    abstract M2: 'T -> unit
    abstract M1: 'T -> unit

type T() =
    interface IA<int> with
        member this.M2(i: int) = ()
        member this.M1(i: int) = ()

    interface IA{caret}<string>
