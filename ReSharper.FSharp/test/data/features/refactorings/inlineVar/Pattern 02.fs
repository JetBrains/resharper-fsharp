type E =
    | A = 1

let [<Literal>] A{caret} = E.A

match A with
| A -> ()
