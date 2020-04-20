module Module

open System

let printerFun{caret} (a : string) b = 
    sprintf "%s %d" a (b |> fst)

[<EntryPoint>]
let main argv =
    printerFun "string" (1, 2) |> stdout.WriteLine
    0 