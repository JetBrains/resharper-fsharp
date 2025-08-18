module Module

type R1 = { F1: int }

type R2 = { F2: R1 }

let r2: R2 = ()

{ r2 with F2.F1 = ""{caret} }
