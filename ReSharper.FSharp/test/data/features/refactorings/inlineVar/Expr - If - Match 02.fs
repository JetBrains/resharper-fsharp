do
    let x = if true then 1 else 2
    match () with
    | _ ->
        x{caret}
