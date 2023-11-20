module Module

let t = MyType()

let _: string = t.P1
let _: string = t.P2
let _: string = t.P3

let _: string = (t :> I).P
