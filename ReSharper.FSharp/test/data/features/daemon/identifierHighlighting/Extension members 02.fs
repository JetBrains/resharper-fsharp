open System
open System.Linq

type Char with
    static member IsZero x = x = char 0

let linq (x : string) =
    x.Select(Char.IsZero).ToArray()
