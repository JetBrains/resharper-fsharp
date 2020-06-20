open System

type T1 = int -> int

let f1 (f : T1) = f 1
let f2 (f : Func<int, int>) = f.Invoke(1)
let f3 (f : int -> int) = f 1
