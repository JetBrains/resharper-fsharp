module Module

type Foo() = class end

type T() =
    member val Prop: Foo{caret} = Foo() with get, set

let i: Foo = T().Prop
