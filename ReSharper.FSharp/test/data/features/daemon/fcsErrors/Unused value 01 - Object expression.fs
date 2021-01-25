module Module

open System
 
{ new IDisposable with member x.Dispose() = () }
