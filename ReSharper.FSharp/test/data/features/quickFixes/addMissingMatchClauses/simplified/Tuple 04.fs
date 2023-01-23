module Say

type U =
    | A
    | B of bool

match A{caret}, true with
| B true, true -> ()
| B false, true -> ()
| B _, _ -> ()
