type R = { F1: int; F2: int; F3: int; F4: int }

match { F1 = 1; F2 = 2; F3 = 3; F4 = 4 } with
| { F1 = f1; F2 = {caret}f2
    F3 = f3; F4 = f4 } -> ()
