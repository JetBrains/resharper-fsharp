module Module

type U =
    | A of named: int

let a = A(""{caret})
