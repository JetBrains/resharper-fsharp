type U2 =
    | A2
    | B2 of int * named: int

match A2 with
| B2(na{caret}) 
