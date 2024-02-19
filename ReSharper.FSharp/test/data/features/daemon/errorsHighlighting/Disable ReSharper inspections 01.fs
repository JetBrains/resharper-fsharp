module Module

let _ = id 1

// ReSharper disable once FSharpRedundantApplication
let _ = id 1

let _ = id 1

// ReSharper disable FSharpRedundantApplication
let _ =
    id 1 |> ignore
    id 1
// ReSharper restore FSharpRedundantApplication

let _ = id 1

// ReSharper disable FSharpRedundantApplication

let _ = id 1
let _ = id 1

let _ = fun x -> x
