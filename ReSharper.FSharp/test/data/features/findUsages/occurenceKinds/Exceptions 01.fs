module Module

exception Exn1
with static member Foo = 123

Exn1
Exn1.Foo

exception Exn2 of int
with static member Foo = 123

Exn2(123)
Exn2.Foo
