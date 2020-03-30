open System

type FooAttribute() = inherit Attribute()

[<FooAttribute>]
type A =
    class end
