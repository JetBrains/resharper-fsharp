module Module

type R1 =
    { A: int }

type R2 =
    { ...R1
      B: int }

type R3{on} =
    { ...R2
      C: int }


let r: R3 = Unchecked.defaultof<_>
let _ = { r with A = 1 }
