module Module

let{off} f{off} x{on}: int = x + 1
let{off} g{off} (Some{off}(x{on})) = x + 1

let{off} (x{off}) = 1

type A(x{on}) =
    member{off} this{off}.M{off}(y{on}) = x + y
