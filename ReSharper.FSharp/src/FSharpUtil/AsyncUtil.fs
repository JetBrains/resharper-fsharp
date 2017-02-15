module FSharpUtil.AsyncUtil

open System.Threading

[<CompiledName("RunAsyncAndSetEvent")>]
let inline runAsyncAndSetEvent fsAsync (mres : ManualResetEventSlim) = async {
    let result = Async.RunSynchronously fsAsync
    mres.Set()
    return result
}
