module Module

type R1 =
    { A{on}: int }

type R2 =
    { ...R1
      B: int }

type R3 =
    { ...R2
      C: int }


let r: R3 = Unchecked.defaultof<_>
let _ = { r with A{on} = 1 }
