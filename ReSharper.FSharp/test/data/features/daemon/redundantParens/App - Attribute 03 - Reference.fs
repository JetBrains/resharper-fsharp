module M

let [<Literal>] S = "X"

[<CompiledName(S)>]
let x = 123
