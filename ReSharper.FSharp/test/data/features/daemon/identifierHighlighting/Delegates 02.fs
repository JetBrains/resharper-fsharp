open System

type T1 = int -> int
type T2 = T1
type T3 = T2

let f1 (f : T1) = f 1
let f2 (f : T3) = f 1
let f3 (f : int -> int) = f 1
let f4 (f : Func<int, int>) = f.Invoke(1)
