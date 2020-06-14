open System
open System.Linq

type Int32 with
    static member MoreThanZero x = x > 0

let showLinq = (Array.zeroCreate<int> 0).Where(Int32.MoreThanZero).Select(fun x -> x + 1).Distinct().ToArray()