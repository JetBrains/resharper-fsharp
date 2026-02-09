[<AutoOpen>]
module Module

open System

let disposable =
    { new IDisposable with member this.Dispose() = () }
