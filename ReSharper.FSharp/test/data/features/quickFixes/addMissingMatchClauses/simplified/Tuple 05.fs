module Say

type U =
    | A of bool

match A{caret} true, true with
| A true, true -> ()
| A false, true -> ()
