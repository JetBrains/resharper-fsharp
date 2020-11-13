module Module

open System

type Delegate = delegate of int -> int

{caret}new Delegate(fun _ -> 1) |> ignore
