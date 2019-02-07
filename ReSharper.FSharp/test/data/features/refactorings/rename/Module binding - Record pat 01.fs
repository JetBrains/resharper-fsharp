module Module

type R = { F: int }

let { F = x } = { F = 123 }
let y = x{caret} + x
