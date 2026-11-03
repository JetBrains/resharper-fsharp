type T(a: int, b: int) =
    new(a: int) = T(a, 0)
    member val P = 0 with get, set

let t = T(a = 1, P = {caret})
