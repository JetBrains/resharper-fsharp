//${RUN:1}
type FooAttribute() =
    inherit System.Attribute()

let foo ([<Foo>] x, y{caret}) = ()
