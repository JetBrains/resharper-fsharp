module Module

open System

let printerFun{caret} a b = sprintf "%s %d" a b

[<EntryPoint>]
let main argv =
    printerFun "string" (int64 1) |> stdout.WriteLine
    0 