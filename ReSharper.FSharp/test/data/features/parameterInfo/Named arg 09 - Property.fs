type T(a: int, b: int) =
    member val P = 0 with get, set

let t = T(a = 1, P = 2{caret})
