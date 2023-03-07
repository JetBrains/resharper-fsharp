type R = | R of a: int * b: int * c: int
match R(0, 1, 2) with
| R(a = a; b = b; c= c{caret}) -> ()