module Module

let f{caret} (a: string) b =
    let b1, (_, b3) = b
    sprintf "%s %d %s" a b1 b3
