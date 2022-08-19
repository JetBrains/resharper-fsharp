type U =
    | A of named: int * string

match A 1 with
| A (1, ""{caret}) -> ()
