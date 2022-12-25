type R = | R of a: int * b: int
match R(0, 8) with
| R(a= a{caret}; b= b) -> ()
