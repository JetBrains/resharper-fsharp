module Module

let f i = fun (j: int) -> i + j + 1

f ""{caret} 1
