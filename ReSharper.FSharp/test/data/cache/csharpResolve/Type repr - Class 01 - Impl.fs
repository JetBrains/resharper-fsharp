namespace global

type T() =
    class
        interface System.IDisposable with
            member x.Dispose() = ()
    end
