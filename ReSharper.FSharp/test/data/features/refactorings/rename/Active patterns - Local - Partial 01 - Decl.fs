module Module

let x =
    let (    |  B{caret}  |    _ | ) x =
        if x then Some () else None

    match true with
    | B
    | _ -> ()
