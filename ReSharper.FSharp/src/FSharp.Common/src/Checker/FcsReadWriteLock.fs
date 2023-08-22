/// Guards access to FSharp.Compiler.Service project model built on top of Project and Psi model snapshots
module JetBrains.ReSharper.Plugins.FSharp.Checker.FcsReadWriteLock

open System
open JetBrains.Application.Threading
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Util.Concurrency

let private fcsLocks = ReentrantWriterPreferenceReadWriteLock()

let assertReadAccess () =
    Assertion.Assert(fcsLocks.IsReadLockHeldByCurrentThread(), "FcsReadWriteLock.assertReadAccess")

let assertWriteAccess () =
    Assertion.Assert(fcsLocks.IsWriteLockHeldByCurrentThread(), "FcsReadWriteLock.assertWriteAccess")


[<Struct>]
type ReadCookie =
    interface IDisposable with
        member this.Dispose() = fcsLocks.ReadLock.Release()

    static member Create(): IDisposable =
        fcsLocks.ReadLock.Acquire()
        new ReadCookie()

[<Struct>]
type WriteCookie =
    interface IDisposable with
        member this.Dispose() = fcsLocks.WriteLock.Release()

    static member Create(locks: IShellLocks): IDisposable =
        let mutable acquired = false

        while not acquired do
            if fcsLocks.WriteLock.TryAcquire(0) then
                acquired <- true
            elif locks.IsReadAccessAllowed() then
                FSharpAsyncUtil.ProcessEnqueuedReadRequests()

        new WriteCookie()
