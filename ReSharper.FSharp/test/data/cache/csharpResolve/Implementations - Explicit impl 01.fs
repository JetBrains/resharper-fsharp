module Module

open System

type T() =
    interface IDisposable with
        member x.Dispose() = ()
