open System

let d: IDisposable = Unchecked.defaultof<_>
d.Dispose{on}() 
  
{ new IDisposable with
      member x.Dispose() = () }
