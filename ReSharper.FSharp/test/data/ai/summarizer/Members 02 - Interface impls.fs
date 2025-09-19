module Module

type Type =
    interface System.Collections.Generic.IList<int> with
        member this.Add(x) = ()

    interface System.IDisposable with
        member this.Dispose() = ()
