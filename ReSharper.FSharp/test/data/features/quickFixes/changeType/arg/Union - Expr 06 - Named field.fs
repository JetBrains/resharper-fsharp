module Module

type U =
    | A of named: int * other: int

let a = A(named = ""{caret}, other = 1)
