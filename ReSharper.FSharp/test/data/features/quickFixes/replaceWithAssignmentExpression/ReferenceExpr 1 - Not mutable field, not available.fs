type MyType =
    val Field: int
    new() = {Field = 0}

let f() =
    let myType = MyType()
    myType.Field = 5{caret}
    ()
