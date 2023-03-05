type R = { F1: int }
match { F1 = 1 } with
| { F1 = f1{caret} } -> ()
