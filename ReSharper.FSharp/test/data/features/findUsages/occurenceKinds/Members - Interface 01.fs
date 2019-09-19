module Module

type T() =
    interface System.IDisposable with
        member x.Dispose() = ()
