module Module

[<Struct>]
type T =
    | A of int

type T with
    member x.Foo = 123
