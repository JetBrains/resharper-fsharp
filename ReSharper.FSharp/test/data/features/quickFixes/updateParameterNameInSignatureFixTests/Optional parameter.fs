module Foo

type MyAttribute() =
    inherit System.Attribute()

type Foo =
    static member Bar(?v{caret}: int) = 0