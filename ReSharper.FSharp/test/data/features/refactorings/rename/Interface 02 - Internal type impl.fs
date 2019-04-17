module Module

type IInterface =
    abstract Foo: int

type internal T() =
    interface IInterface with
        member x.Foo{caret} = 123