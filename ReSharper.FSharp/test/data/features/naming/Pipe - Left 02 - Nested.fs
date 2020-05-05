module Module

let getName _ _ = ()
let foo _ = ()
let x{caret} = getName <| foo <| 1
