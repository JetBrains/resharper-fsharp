module Module

open System

let f a b = ()
do
    f {selstart}{new IDisposable with member x.Dispose() = ()}{selend}{caret}id
