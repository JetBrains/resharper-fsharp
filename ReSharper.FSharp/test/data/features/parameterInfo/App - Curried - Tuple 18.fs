open System

let f (a, [<ParamArray>] b: int[]) = ()

f (1, 2, {caret}3)
