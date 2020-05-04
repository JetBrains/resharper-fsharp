module Mod

open System.Linq

let x = Seq.empty<string * int>
{selstart}Enumerable.ToDictionary(x, (fun (x, y) -> x), (fun (x, y) -> y)){selend}
