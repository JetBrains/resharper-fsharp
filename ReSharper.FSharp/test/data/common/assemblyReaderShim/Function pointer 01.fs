module Module

let c = Class()

let _: nativeint = c.Field
let _: nativeint = c.Property
let _: nativeint = Class.N()

Class.M(0n)


let _: int = c.Field
let _: int = c.Property
let _: int = Class.N()

Class.M("")
