module Mod

open System.Linq

let x = Seq.empty<string * int>
{selstart}x.ToDictionary((fun (x, y) -> x), (fun (x, y) -> y)){selend}
