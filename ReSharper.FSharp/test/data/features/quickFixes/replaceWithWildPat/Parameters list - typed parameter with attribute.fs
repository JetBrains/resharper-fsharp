//${RUN:1}
type FooAttribute() =
    inherit System.Attribute()

let foo ([<Foo>] x: int, y{caret}: int) = ()
