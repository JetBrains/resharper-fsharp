let f a b =
    if{caret} a then
        match b with
        | Some x -> x
        | None -> 0
    else
        1
