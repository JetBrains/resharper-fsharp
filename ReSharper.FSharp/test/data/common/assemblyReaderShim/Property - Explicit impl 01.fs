module Module

let c1 = C1()

let iInt = c1 :> I<int>
let i: int = iInt.P

let iString = c1 :> I<string>
let s: string = iString.P


let c2 = C2<unit>()
let iUnit = c2 :> I<unit>
let u: unit = iUnit.P
