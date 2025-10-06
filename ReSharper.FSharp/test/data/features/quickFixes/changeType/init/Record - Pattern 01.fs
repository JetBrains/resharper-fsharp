module Module

type R = { Field: int }

match { Field = 1 } with
| { Field = ""{caret} } -> ()
