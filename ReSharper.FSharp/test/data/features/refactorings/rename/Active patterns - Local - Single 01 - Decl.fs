module Module

let x =
    let (    | Not{caret} | ) x =
        not X

    let f (Not x) = x

    match true with
    | Not x -> ()
