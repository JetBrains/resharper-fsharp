type R = | R of F1: int * F2: int * F3: int
match R(1,2,3) with
| R(a = a // I'm a comment
    b = b{caret}; c= c) -> ()
