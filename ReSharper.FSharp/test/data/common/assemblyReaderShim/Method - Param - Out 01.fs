module Module

let mutable x = 1
Class.M(&x)

let _: int = Class.M()

Class.M("")
