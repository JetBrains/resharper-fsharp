/// Guards access to FSharp.Compiler.Service project model built on top of Project and Psi model snapshots
module JetBrains.ReSharper.Plugins.FSharp.Checker.FcsReadWriteLock

open System
open System.Threading
open JetBrains.Diagnostics
open JetBrains.Util.Concurrency

let private locks = ReentrantWriterPreferenceReadWriteLock()

let assertReadAccess () =
    Assertion.Assert(locks.IsReadLockAcquired(Thread.CurrentThread), "FcsReadWriteLock.assertReadAccess")

let assertWriteAccess () =
    Assertion.Assert(locks.IsWriteLockAcquired(Thread.CurrentThread), "FcsReadWriteLock.assertWriteAccess")


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
