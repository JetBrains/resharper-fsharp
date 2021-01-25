do
    let foo _ = ()

    let mutable i = 123
    i{caret} <- 1
    foo &i
