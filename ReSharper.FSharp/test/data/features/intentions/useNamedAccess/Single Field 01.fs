type A = | A of a: int

match A(1) with
| A(_{caret}) -> ()
