type R1 = { A: int; B: int }

type R2 =
    { ...R1
      C: int
      D: int }

let r1: R1 = Unchecked.defaultof<_>

let r =
    {caret}{ ...r1
      C = 1 }
