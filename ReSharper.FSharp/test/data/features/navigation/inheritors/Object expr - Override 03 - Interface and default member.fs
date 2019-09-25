let d: System.IDisposable = Unchecked.defaultof<_>
d.Dispose{on}()

type T() =
    abstract P: int
    default x.P = 1

{ new T() with
      override x.P = 1

  interface System.IDisposable with
      member x.Dispose() = () }
