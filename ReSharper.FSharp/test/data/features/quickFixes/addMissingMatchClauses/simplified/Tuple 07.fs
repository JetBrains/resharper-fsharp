module Say

type U =
    | A of bool

match true, A{caret} true with
| true, A true -> ()
| true, A false -> ()
