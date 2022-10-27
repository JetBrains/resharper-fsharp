module Module

type R1 =
    { F1: int
      F2: int
      F3: int }

type R2 =
    { F4: int }

if true then { F1 = 1 } else { {caret} }
