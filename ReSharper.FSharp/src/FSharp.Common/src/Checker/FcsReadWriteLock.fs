module JetBrains.ReSharper.Plugins.FSharp.Checker.FcsReadWriteLock

open System
open JetBrains.Util.Concurrency

let private locks = ReentrantWriterPreferenceReadWriteLock()

[<Struct>]
type ReadCookie =
    interface IDisposable with
        member this.Dispose() = locks.ReadLock.Release()

    static member Create(): IDisposable =
        locks.ReadLock.Acquire()
        new ReadCookie()

[<Struct>]
type WriteCookie =
    interface IDisposable with
        member this.Dispose() = locks.WriteLock.Release()

    static member Create(): IDisposable =
        locks.WriteLock.Acquire()
        new WriteCookie()
