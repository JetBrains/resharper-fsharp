module Module

let x =
    let (    |  B  |    C | ) x =
        if x then B{caret} else C

    match true with
    | B
    | C -> ()
