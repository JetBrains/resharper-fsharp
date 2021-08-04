module Module

type IInterface{on} =
    abstract P: int

type TType =
    | C
    interface IInterface with
        member this.P = failwith "todo"
