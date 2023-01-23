module Say

type R =
    { F1: int
      F2: int }

match None{caret} with
| Some { F1 = 1 } -> ()
