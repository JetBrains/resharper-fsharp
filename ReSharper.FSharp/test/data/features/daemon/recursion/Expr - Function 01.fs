module Module

let rec f1 x = function
    | 0 -> ()
    | n -> f1 x (n - 1)

let rec f2 x =
    fun y -> f2 x y

let rec f3 x =
    match x with
    | 0 -> ()
    | n -> f3 (n - 1)
