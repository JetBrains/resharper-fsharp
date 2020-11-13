module Module1 =
    type Delegate = delegate of int -> int

open System
open Module1

Module1.Delegate(fun _ -> 1)
