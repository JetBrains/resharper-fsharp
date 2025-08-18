module Module

type R = { Field: int }

let r: R = { Field = 1 }

let f (p: string) = ()

f r.Field{caret}
