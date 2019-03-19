module Module

let x =
    let (    |  B  |    _ | ) x =
        if x then Some () else None

    match true with
    | B{caret}
    | B
    | _ -> ()
