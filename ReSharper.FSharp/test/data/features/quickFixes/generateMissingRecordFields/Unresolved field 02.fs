module Module

type R1 =
    { F1: int
      F2: int }

let r: R1 = { F = 1
              F1 = 2{caret} }
