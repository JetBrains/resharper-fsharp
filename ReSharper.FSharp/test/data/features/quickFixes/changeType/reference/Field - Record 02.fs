module Module

type R = { Field: int }

let r: R = { Field = 1 }

let s: string = r.Field{caret}
