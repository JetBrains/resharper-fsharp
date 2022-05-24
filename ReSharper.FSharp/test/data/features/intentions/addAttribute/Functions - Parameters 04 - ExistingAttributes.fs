module Test =
    type MyAttr() = inherit System.Attribute()
    let f ([<MyAttr; MyAttr>] a{caret}) = ()
