module Module

type T(a{caret}, b) =
    let _ = a

let t = T(a = 1)
