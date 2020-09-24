type MyType() =
    [<DefaultValue>] val mutable Field: int

let f() =
    let myType = MyType()
    myType.Field = 5{caret}
    ()
