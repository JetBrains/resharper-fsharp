module Module

type E =
    | Case1 = 1
    | Case2 = 2

let (e1: E) = {caret}E.Case1
let (e2: E) = E.Case2
