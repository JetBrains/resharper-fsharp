module Module

let a: decimal = Class.Dec
let b: int = Class.Int
let c: decimal = Class.Ro

[<Literal>]
let L = Class.Dec

let d x = match x with | Class.Dec -> 1 | _ -> 2
