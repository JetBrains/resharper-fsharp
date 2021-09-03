module Module

let x =
    let (    |  B  |    C{caret} | ) x =
        if x then B else C

    match true with
    | B
    | C -> ()

    let _ = (|B|C|)

    let (|Id|) f x = x

    match () with
    | Id (|B|C|) 1 -> ()
