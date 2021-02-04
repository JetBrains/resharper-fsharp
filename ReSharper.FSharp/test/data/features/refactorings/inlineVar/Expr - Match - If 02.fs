do
    let x = match () with _ -> if true then () else ()
    if true then x else x{caret}
