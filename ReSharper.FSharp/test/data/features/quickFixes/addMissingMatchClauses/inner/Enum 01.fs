module Say

type E =
    | A = 1
    | B = 2

match E.A{caret} with
| E.A -> ()