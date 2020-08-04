module Module

match (1, 2) with
| ({caret}x, y) when y > 5 -> ()
| _ -> ()
