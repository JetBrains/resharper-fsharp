module Say

type E =
    | A = 1
    | B = 2
    | C = 3

match E.A{caret} with
| E.A & E.B -> ()
