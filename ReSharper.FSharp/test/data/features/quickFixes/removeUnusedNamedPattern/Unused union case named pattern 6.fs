type R = | R of F1: int * F2: int * F3: int
match R(1,2,3) with
| R(a = a
    b = b
    c= c{caret}) -> ()

