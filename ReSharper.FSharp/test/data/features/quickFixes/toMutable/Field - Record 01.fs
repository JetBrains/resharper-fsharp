module M

type R =
    { F: int }

let r = { F = 123 }
r.F{caret} <- 123
