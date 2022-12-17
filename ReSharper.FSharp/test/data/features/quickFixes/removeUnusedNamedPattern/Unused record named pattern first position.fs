type R = { F1: int; F2: int; F3: int }
match { F1 = 1; F2 = 2; F3 = 3 } with
| { F1 = f1{caret}; F2 = 2; F3 = 3 } -> ()
| { F1 = 1; F2 = f2; F3 = 3 } -> ()
| { F1 = f1; F2 = f2; F3 = f3 } -> ()
