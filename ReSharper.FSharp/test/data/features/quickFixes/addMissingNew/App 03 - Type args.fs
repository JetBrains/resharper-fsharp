module Module

open System

type Disposable<'T>() =
    interface IDisposable with
        member this.Dispose() = ()

let disposable = Dispos{caret}able<int>()
