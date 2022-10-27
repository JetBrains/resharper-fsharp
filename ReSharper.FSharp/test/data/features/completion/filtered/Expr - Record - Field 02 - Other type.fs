module Module

type R1 =
    { F1: int
      F2: int
      F3: int }

type R2 = 
    { F1: int 
      F4: int }

{ F1 = 1
  {caret} }
