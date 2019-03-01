module Module

type T(a: int) =
    new() = T(123)

let t1 = T()
let t2 = T(123)
let t3 = T{caret}(123)
