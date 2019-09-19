module Module

type R = { mutable F: int }
let r1 = { F = 1 }
let r2 = { r1 with F = r1.F + 1 }
