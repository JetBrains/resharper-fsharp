module Module

type R =
    { Field: int }
    interface System.IDisposable with
        member x.Dispose() = ()
