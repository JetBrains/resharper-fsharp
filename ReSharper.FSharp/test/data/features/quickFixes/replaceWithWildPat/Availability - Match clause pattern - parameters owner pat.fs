module Module

match Some((1, 2)) with
| Some({caret}x, y) when y > 5 -> ()
| _ -> ()
