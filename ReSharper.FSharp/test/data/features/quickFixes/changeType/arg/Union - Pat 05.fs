module Module

type U =
    | A of named: int

match A 1 with
| A(named = ""{caret}) -> ()
