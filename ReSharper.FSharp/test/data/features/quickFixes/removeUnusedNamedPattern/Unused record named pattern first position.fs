type R = { F1: int; F2: int }
match { F1 = 1; F2 = 2 } with
| { F1 = f1{caret}; F2 = 2 } -> ()
