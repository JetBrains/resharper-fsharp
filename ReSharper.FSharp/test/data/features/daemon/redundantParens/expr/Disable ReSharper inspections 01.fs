module Module

// ReSharper disable once FSharpRedundantParens
let _ =
    (1)

let _ =
    // ReSharper disable once FSharpRedundantParens
    (
     1), (1)

let _ =
    // ReSharper disable FSharpRedundantParens
    ((1), (1)) |> ignore
    // ReSharper restore FSharpRedundantParens
    (1)
