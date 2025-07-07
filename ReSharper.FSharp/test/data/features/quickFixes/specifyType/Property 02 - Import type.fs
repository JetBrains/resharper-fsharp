type A(b: B) =
    let _ = b.X.Assembly{caret}

and B() =
    member _.X = "".GetType()
