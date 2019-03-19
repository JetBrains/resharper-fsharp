module Module

type C<'T>() =
    [<DefaultValue>] val mutable Foo: int

let t = C<_>()
t.Foo <- t.Foo + 1

C<_>().Foo <- 123
