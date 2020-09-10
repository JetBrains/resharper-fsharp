type FooAttribute() =
    inherit System.Attribute()

let foo ([<Foo>] (x{caret})) =
    ()
