module Module

type R = { A: int }

let f { A = a{caret} } = ()
