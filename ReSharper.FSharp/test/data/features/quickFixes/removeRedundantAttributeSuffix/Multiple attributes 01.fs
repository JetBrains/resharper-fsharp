open System

type FooAttribute() = inherit Attribute()
type BarAttribute() = inherit Attribute()

[<FooAttribute{caret}; Bar>]
type A =
    class end
