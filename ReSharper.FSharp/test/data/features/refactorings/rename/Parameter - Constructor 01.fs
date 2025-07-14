module Module

type T(a{caret}) =
    let _ = a

let t = T(a = 1)
