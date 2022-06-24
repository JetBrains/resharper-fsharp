type T() =
    let f x = x + 1 |> string
    do f {caret}
