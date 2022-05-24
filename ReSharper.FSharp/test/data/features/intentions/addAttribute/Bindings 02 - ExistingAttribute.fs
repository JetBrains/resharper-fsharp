module Test =
    type MyAttr() = inherit System.Attribute()
    [<MyAttr>]
    let f{caret}() = ()