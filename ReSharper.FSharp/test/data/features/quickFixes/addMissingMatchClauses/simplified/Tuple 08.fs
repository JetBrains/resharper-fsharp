module Say

type U =
    | A of bool

match true, A{caret} true with
| false, A true -> ()
| false, A false -> ()
