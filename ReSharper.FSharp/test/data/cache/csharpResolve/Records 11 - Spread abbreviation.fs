module Module

type R1 =
    { A: int }

type R1Abbr = R1

type R2 =
    { ...R1Abbr
      B: int }
