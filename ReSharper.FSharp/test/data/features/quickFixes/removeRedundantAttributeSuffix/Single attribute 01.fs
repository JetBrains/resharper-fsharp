open System

type FooAttribute() = inherit Attribute()

[<FooAttribute{caret}>]
type A =
    class end
