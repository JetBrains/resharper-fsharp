module Module

let getName _ = ()
let foo _ = ()
let x{caret} = getName <| foo <| 1
