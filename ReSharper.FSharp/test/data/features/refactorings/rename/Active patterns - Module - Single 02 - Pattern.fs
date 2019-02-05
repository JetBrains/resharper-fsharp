module Module

let (    | Not | ) x =
    not X

let f (Not{caret} x) =
    let (Not x) = x
    x

match true with
| Not x -> ()
