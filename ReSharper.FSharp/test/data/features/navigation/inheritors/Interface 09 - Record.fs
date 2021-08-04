module Module

type IInterface{on} =
    abstract P: int

type TType =
    { F: int }
    interface IInterface with
        member this.P = failwith "todo"
