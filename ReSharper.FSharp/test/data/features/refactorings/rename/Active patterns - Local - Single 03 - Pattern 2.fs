module Module

let x =
    let (    | Not | ) x =
        not X

    let f (Not x) = x

    match true with
    | Not{caret} x -> ()

    let _ = (|Not|)

    let (|Id|) f x = x

    match () with
    | Id (|Not|) 1 -> ()
