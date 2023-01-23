module Say

type U =
    | A
    | B

match None{caret} with
//| None -> ()
| Some A -> ()
| Some B -> ()
