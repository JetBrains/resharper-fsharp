module Module

let x =
    let (    |  B{caret}  |    _ | ) x =
        if x then Some () else None

    match true with
    | B
    | _ -> ()

    let _ = (|B|_|)

    let (|Id|) f x = x

    match () with
    | Id (|B|_|) 1 -> ()
