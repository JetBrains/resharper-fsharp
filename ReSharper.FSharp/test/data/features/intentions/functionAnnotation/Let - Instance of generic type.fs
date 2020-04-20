module Module

open System

let printerFun{caret} a = 
    String.concat ", " a

[<EntryPoint>]
let main argv =
    printerFun ["first"; "second"] |> stdout.WriteLine
    0 