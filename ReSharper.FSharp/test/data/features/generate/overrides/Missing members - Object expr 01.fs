// ${KIND:Implementations}
// ${SELECT0:Foo(System.Int32,System.Int32):System.Int32}
// ${SELECT1:Bar(System.String):System.String}

type Interface =
    abstract member Foo : int -> int -> int
    abstract member Bar : string -> string

let foo =
    { new Interface with{caret} }
