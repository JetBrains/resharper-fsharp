module Module

open System

let printerFun{caret}() = "foo bar" |> stdout.WriteLine

[<EntryPoint>]
let main argv =
    printerFun()
    0 