module Module

let c = C(1)

let i1: int = c.P1
let s11: string = c.P1
let s12: string = c.P2


let pi: I = c :> I

let i2: int = pi.P1
let s21: string = pi.P1
let s22: string = pi.P2
