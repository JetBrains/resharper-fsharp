module Module

type R = { mutable Field: int }

let r = { Field = 123 } 
let r1 = { R.Field = 123 }
let r2 = { r1 with Field = r1.Field + 1 }

r2.Field <- r1.Field
