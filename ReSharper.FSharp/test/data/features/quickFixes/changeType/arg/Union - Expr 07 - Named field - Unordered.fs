module Module

type U =
    | A of named: int * other: int

let a = A(other = ""{caret}, named = 1)
