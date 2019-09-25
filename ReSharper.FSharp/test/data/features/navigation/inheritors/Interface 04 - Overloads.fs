module Module

type IInterface =
    abstract Foo{on}: int -> unit
    abstract Foo: double -> unit

type T() =
    interface IInterface with
        member x.Foo(i: int) = ()
        member x.Foo(d: double) = ()
