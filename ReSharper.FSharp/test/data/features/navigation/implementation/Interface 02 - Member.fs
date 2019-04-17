module Module

type IInterface =
    abstract Foo{on}: int

type T() =
    interface IInterface with
        member x.Foo = 123
