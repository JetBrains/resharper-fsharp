type Foo() = class end

type T() =
    [<DefaultValue>] val mutable F: Foo{caret}

T().F
