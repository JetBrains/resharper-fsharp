type A = | A of a: int

match A(1) with
| A(2{caret}) -> ()
