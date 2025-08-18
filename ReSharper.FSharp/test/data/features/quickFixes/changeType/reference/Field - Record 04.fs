module Module

type R = { Field: int }

let r: R = { Field = 1 }

r.Field{caret} + "" |> ignore
