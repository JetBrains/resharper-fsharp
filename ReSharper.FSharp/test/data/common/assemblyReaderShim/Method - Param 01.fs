module Module

let mutable b = true

let s1: string = Class.M(true)
let (s2: string), (b1: bool) = Class.M1()
let (s3: string) = Class.M2(&b)
