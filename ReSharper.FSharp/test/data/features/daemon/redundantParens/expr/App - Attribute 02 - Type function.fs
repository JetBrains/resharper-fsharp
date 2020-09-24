module M

type FooAttribute(t: System.Type) =
    inherit System.Attribute()

[<Foo(typeof<int>)>]
let x = 123
