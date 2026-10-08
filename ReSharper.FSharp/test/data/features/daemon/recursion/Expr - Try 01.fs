module Module

let rec f1 x =
    try f1 x with _ -> 0

let rec f2 x =
    try 0 with _ -> f2 x

let rec f3 x =
    try f3 x finally ()
