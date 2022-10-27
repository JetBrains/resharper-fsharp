module Module

type R1 =
    { F1: int
      F2: int
      F3: int }

type R2 =
    { F4: int
      R: R1 }

{ R = { {caret} } }
