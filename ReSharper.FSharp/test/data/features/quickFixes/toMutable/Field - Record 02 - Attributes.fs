module M

type R =
    { [<CompiledName("G")>] F: int }

let r = { F = 123 }
r.F{caret} <- 123
