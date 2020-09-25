let f() =
    let a = [|0|]
    a.[0] = 5{caret}
    ()
