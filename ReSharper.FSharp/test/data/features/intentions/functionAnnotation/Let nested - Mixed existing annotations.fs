module Module

open System

[<EntryPoint>]
let main argv =
    let printerFun{caret} (a:string) b = sprintf "%s %d" a b
    printerFun "string" (int64 1) |> stdout.WriteLine
    0 