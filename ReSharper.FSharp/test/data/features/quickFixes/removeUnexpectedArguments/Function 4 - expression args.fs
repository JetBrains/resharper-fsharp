open System

let f (x: IDisposable) = ()
let g = (f ({ new IDisposable with
              member this.Dispose() = ()
           }) {caret}2 4)