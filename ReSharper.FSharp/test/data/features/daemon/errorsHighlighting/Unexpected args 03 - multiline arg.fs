module Module

open System

let g = (id 2) ({ new IDisposable with
                  member this.Dispose() = ()
                })