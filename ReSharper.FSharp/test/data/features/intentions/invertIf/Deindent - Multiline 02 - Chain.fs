let f a =
    if{caret} a then None else

    let x = g ()
    if x then Some 1 else

    let y = h ()
    Some y
