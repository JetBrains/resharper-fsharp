type FooAttribute() =
    inherit System.Attribute()

let a : [<Foo>] int = ""{caret}