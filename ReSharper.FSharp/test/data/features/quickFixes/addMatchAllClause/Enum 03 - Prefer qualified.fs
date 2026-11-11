module Say

type Enum =
    | A = 1

match Enum.A{caret} with
| Enum.A -> ()
