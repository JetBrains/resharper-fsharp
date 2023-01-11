module Module

let c1 = C1()
let i1 = c1 :> I1
i1.M()


let c2 = C2()

let i2Int = c2 :> I2<int>
let i: int = i2Int.M()

let i2String = c2 :> I2<string>
let s: string = i2String.M()


let c3 = C3<unit>()
let i2 = c3 :> I2<unit>
let u: unit = i2.M()
