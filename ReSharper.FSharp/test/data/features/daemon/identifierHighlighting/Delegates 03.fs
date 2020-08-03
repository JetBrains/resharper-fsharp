open System

type T1 = int -> int
type T2 = T1
type T3 = T2
type T4 = T3

let f1 (f : T4) = f 1

