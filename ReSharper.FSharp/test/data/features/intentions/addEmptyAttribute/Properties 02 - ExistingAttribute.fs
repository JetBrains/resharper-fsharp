module Test =
    type MyAttr() = inherit System.Attribute()
    type A() =
        [<MyAttr>]
        member _.F{caret} = ()