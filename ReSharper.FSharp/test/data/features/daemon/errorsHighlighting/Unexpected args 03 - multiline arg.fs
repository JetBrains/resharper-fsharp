module Module

open System

let f x = ()
let g = f 2 ({ new IDisposable with
              member this.Dispose() = ()
             })