module Test =
    type MyAttr() = inherit System.Attribute()
    let f ([<MyAttr>] a{caret}: int) = ()