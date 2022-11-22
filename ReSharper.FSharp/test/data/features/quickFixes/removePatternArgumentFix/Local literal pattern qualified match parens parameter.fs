type E =
    | A = 1
    | B = 2

match E.A with
| (E.A x{caret}) -> ()
