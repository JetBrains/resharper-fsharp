//${RUN:1}
module Module

match Some((1, 2, 3)) with
| Some({caret}x, y, z) when z > 5 -> ()
