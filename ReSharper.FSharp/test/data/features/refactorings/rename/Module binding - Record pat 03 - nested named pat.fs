module Module

type R1 = { F1: int }
type R2 = { F2: R1 }

let { F2 = { F1 = y } as x } = { F2 = { F1 = 123 } }
let y = x{caret} + x
