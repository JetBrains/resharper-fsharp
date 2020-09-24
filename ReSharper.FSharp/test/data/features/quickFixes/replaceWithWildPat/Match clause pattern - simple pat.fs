//${RUN:1}
match (1, 2, 3) with
| ({caret}x, y, z) when z > 5 -> ()
