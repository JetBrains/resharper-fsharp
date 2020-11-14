namespace Ns

open System

{ new IDisposable{off} with member x.Dispose() = () }
