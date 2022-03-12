module Module

type IA =
    abstract P: int

type A() =
    interface IA with
        member this.P = failwith "todo"

type IB =
    inherit IA

    abstract P2: int
    abstract P3: int

type B() =
    inherit A()

    interface IB{caret} with
        member this.P2 = failwith "todo"
