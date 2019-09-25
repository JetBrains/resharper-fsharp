module Module

type IInterface{on} =
    abstract Foo: int

type T() =
    interface IInterface with
        member x.Foo = 123
