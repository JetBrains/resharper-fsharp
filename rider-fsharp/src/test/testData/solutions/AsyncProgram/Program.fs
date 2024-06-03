open System

let doWork arg = async {
    return arg + " processed"
}

let process msg = async {
    let! value = doWork msg
    return () // breakpoint here
}

[<EntryPoint>]
let main argv =
    process "MyMessage" |> Async.Start |> ignore
    Console.ReadLine()
    0
