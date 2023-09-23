type R = { F1: int; F2: int }
match { F1 = 1; F2 = 2 } with
| { F1 = f1
    F2 = f2{caret} } -> ()
