type Foo() = class end

type T() =
    [<DefaultValue>] val mutable F{caret}: Foo

T().F
