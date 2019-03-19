module Module

let x =
    let (    |  B  |    C | ) x =
        if x then B else C

    match true with
    | B
    | C{caret} -> ()
