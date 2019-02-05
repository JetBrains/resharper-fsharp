module Module

let x =
    let (    |  B  |    C{caret} | ) x =
        if x then B else C

    match true with
    | B
    | C -> ()
