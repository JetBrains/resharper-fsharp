module Module

type R1 =
    { F1: int
      F2: int }

type R2 =
    { ...R1
      F3: int
      F4: int }

let r1: R1 = Unchecked.defaultof<_>

let r: R2 =
    { ...r1
      {caret} }
