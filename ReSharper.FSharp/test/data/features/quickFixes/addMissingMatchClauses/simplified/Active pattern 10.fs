module Say

let (|Id|) x = x

type U1 =
    | A1 of bool

type U2 =
    | A2
    | B2 of U1

match A2{caret} with
| B2(Id _ as A1 b) -> ()
