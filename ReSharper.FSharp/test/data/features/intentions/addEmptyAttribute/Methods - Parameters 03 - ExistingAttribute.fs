module Test =
    type MyAttr() = inherit System.Attribute()
    type A() =
        member _.F1 ([<MyAttr>] a{caret}) = ()