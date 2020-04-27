module Module

open System

let printerFun{caret} (a : string) b = 
    sprintf "%s %d %s" a (b |> fst) (b |> snd |> snd)

[<EntryPoint>]
let main argv =
    printerFun "string" (1, (2, "anotherString")) |> stdout.WriteLine
    0 