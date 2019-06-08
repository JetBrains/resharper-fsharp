module Module

type T() =
    member x.Dispose() = ()
    interface System.IDisposable with
        member x.Dispose() = ()

(new T()).Dispose{on}()
