//${RUN:1}
module Module

type FooAttribute() =
    inherit System.Attribute()

let foo ([<Foo>] x: int, y{caret}: int) = ()
