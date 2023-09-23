type R = | R of a: int * b: int
match R(0, 1) with
| R(a= a; b= b{caret}) -> ()
