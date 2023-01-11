module Module

let n1 = Class.Nested1()

let iInt = n1 :> Class.I<int>
iInt.M()

let iString = n1 :> Class.I<string>
iString.M()


let n2 = Class.Nested2<unit>()
let iUnit = n2 :> Class.I<unit>
iUnit.M()
