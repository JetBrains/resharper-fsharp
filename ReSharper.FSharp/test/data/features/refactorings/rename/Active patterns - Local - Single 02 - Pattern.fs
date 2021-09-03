module Module

let x =
    let (    | Not | ) x =
        not X

    let f (Not{caret} x) =
        let (Not x) = x
        x

    match true with
    | Not x -> ()

    let _ = (|Not|)

    let (|Id|) f x = x

    match () with
    | Id (|Not|) 1 -> ()
