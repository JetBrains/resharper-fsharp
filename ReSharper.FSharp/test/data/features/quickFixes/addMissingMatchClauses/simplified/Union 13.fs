module Say

type U =
    | A of bool * bool
    | B of bool

match None{caret} with
| Some (A (x, y)) -> ()
| Some (B x) -> ()
