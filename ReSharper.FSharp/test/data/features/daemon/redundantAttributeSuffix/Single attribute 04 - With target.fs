open System

type FooAttribute() = inherit Attribute()

[<type:FooAttribute>]
type A =
    class end
