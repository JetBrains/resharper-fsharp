type MyType() =
    member val Member = 1 with get

let f() =
    let myType = MyType()
    myType.Member = 5{caret}
    ()
