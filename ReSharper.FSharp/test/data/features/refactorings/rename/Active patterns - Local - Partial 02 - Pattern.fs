module Module

let x =
    let (    |  B  |    _ | ) x =
        if x then Some () else None

    match true with
    | B{caret}
    | B
    | _ -> ()

    let _ = (|B|_|)

    let (|Id|) f x = x

    match () with
    | Id (|B|_|) 1 -> ()
