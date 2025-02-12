module Module

type R = { A: int }
let { A = (a: int) } = failwith ""
