//${RUN:1}
module Module

type FooAttribute() =
    inherit System.Attribute()

let foo ([<Foo>] x, y{caret}) = ()
