module Module

let rec f x =
    x > 0 && f (x - 1)
