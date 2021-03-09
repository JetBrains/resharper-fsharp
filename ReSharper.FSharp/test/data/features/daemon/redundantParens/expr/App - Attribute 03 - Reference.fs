module M =
    let [<Literal>] S = "X"

[<CompiledName(M.S)>]
let x1 = 123

[<CompiledName (M.S)>]
let x2 = 123

open M

[<CompiledName(S)>]
let x3 = 123

[<CompiledName (S)>]
let x4 = 123