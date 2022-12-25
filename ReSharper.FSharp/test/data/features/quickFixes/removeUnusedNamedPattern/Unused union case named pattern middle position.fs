type R = | R of a: int * b: int * c: int
match R(0, 8, 9) with
| R(a= a; b= b{caret}; c= c) -> ()
