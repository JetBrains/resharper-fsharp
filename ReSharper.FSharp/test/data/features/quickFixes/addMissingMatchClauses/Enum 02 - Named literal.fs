module Say

type E =
    | A = 1
    | B = 2

let [<Literal>] A = E.A

match E.A{caret} with
| A -> ()