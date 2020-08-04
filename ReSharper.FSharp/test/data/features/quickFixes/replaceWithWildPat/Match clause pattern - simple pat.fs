//${RUN:1}
module Module

match (1, 2, 3) with
| ({caret}x, y, z) when z > 5 -> ()
