type R1 = { A: int; B: int }
type R2 = { ...R1; C: int }

let r1: R1 = Unchecked.defaultof<_>
let r: R2 = {caret}{ ...r1 }
