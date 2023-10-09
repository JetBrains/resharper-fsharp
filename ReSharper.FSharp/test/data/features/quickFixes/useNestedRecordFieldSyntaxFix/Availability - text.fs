module Test

type Record0 = { Foo: int; Bar: int }
type Record1 = { Foo: int; Bar: int; Zoo: Record0 }

[<AutoOpen>]
module Module = type Record2 = { Foo: Record1; Bar: Record1 }

let f item = { item with Module.Record2.Foo.Zoo = { item.Foo.Zoo with Foo = 3 } }
