module Module

open System

type Disposable<'T>() =
    interface IDisposable with
        member this.Dispose() = ()

let create: unit -> Disposable<int> = Dispos{caret}able<int>
