open System

type FooAttribute() = inherit Attribute()

[<type: FooAttribute{caret}()>]
type A =
    class end
