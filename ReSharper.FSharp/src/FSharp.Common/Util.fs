namespace JetBrains.ReSharper.Plugins.FSharp.Common

[<AutoOpen>]
module Util =
    open System
    open System.Collections.Generic
    open System.Threading
    open JetBrains.Application
    open JetBrains.Application.Progress
    open JetBrains.Util
    open Microsoft.FSharp.Compiler

    let private interruptCheckTimeout = 30
    let private defaultInterruptChecker = Action(fun _ -> InterruptableActivityCookie.CheckAndThrow())
    
    let inline isNotNull x = not (isNull x)

    type Async<'T> with
        member x.RunAsTask(?interruptChecker) = // todo: cache these methods in fs cache provider
            let interruptChecker = defaultArg interruptChecker defaultInterruptChecker
            let cancellationTokenSource = new CancellationTokenSource()
            let cancellationToken = cancellationTokenSource.Token
            let task = Async.StartAsTask(x, cancellationToken = cancellationToken)

            while not task.IsCompleted do
                let finished = task.Wait(interruptCheckTimeout, cancellationToken)
                if not finished then
                    try interruptChecker.Invoke() // todo: C# way
                    with :? ProcessCancelledException ->
                        cancellationTokenSource.Cancel()
                        reraise()
            task.Result

    type ILogger with
        member x.LogFSharpErrors context errors =
            let messages = Seq.fold (fun s (e : FSharpErrorInfo) -> s + "\n" + e.Message) "" errors
            x.LogMessage(LoggingLevel.WARN, sprintf "%s: %s" context messages)
            
    type IDictionary<'TKey, 'TValue> with
        member x.remove (key : 'TKey) = x.Remove key |> ignore
        member x.add (key : 'TKey, value : 'TValue) = x.Add(key, value) |> ignore
        member x.contains (key : 'TKey) = x.ContainsKey key

    type ISet<'T> with
        member x.remove el = x.Remove el |> ignore
        member x.add el = x.Add el |> ignore
