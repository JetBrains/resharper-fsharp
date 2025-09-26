type R = { A: int }

match { A = 1 } with
| { A = {caret}a } -> ()
