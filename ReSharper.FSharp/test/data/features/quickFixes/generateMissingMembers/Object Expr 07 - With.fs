module Module

type Interface =
    abstract member Foo : int -> int -> int
    abstract member Bar : string -> string

let foo =
    { new Interface{caret} with }
