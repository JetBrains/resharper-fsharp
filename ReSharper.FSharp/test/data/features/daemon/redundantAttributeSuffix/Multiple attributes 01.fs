open System

type FooAttribute() = inherit Attribute()
type BarAttribute() = inherit Attribute()

[<FooAttribute; BarAttribute>]
type A =
    class end
