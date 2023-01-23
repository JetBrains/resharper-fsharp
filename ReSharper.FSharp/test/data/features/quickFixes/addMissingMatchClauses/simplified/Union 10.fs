module Say

type U =
    | A of bool * bool

match None{caret} with
| Some (A (x, true)) -> ()
