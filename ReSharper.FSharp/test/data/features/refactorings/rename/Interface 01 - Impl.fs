module Module

type IInterface =
    abstract Foo: int

type T() =
    interface IInterface with
        member x.Foo{caret} = 123
