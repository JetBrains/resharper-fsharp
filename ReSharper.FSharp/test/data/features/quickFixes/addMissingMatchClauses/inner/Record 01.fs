module Say

type R =
    { F1: int
      F2: int }

match { F1 = 1; F2 = 2 }{caret} with
| { F1 = 1 } -> ()
