module Module

open System

let f (x: #IDisposable) =
    match x with
    | {caret}
