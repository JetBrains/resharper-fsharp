module Module

let f() =
    let x, y, (z1{caret}, z2) = ("foo", 1, (1,2))
    ()