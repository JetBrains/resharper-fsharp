type R = { F1: int; F2: int }
match { F1 = 1; F2 = 1 } with
| { F2 = f2{caret} } -> ()
| { F1 = 1; F2 = f } -> ()
| { F1 = f1; F2 = f2 } -> ()
