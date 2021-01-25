{ new T() with
      member x.P = 1

  interface IDisposable with 
      member x.Dispose() = () }
