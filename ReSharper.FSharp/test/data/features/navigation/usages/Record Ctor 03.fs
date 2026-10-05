module Module

type R1 =
    { A: int }

type R2{on} =
    { ...R1
      B: int }

let r: R2 = Unchecked.defaultof<_>
let _ = { r with A = 1 }
