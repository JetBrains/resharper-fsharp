module Module

let rec f1 x =
    x > 0 && f1 (x - 1)

let rec f2 x =
    x > 0 || f2 (x - 1)

let rec f3 x =
    f3 (x - 1) && x > 0

let rec f4 x =
    x > 0 && x < 10 && f4 (x - 1)

let rec f5 x =
    x > 0 && (if x > 1 then f5 (x - 1) else true)

let rec f6 x =
    x > 0 && f6 (x - 1)
    true

let rec f7 x =
    match x with
    | 0 -> true
    | _ -> x > 0 && f7 (x - 1)
