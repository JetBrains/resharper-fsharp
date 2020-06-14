open System
open System.Threading.Tasks

let asyncCex = async { let! a = Task.Run(fun x -> 10) |> Async.AwaitTask
                       a |> ignore } |> Async.RunSynchronously

let seqCex = seq { 0; 1; 2; 3; 4; 5 }