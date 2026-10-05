module Module

type R1 =
    { A: int }
  
type R2 =
    { ...R1
      B: string }

type R3 =
    { ...R2
      C: bool }
