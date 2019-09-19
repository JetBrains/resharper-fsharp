module Module

exception Exn of int
with static member Foo = 123

Exn.Foo
