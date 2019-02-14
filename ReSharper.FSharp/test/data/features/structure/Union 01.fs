module Module

type U =
    | A
    | B of int * named: int
    interface System.IDisposable with
        member x.Dispose() = ()
