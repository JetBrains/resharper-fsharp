module Module

type R1 =
    { F1: int
      F2: int }

let r: R1 = { F1 = 1
              F = 2{caret} }
