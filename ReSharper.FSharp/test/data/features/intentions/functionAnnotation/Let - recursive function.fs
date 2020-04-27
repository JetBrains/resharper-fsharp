module Module

open System

let rec printerFun{caret} a = sprintf "%s" a

[<EntryPoint>]
let main argv =
    printerFun "string" (int64 1) |> stdout.WriteLine
    0 