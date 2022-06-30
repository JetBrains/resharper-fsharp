open System

let f ([<ParamArray>] a: int[]) = ()

f (1, 2 {caret})
