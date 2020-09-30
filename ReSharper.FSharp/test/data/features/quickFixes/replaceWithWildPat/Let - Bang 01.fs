async {
    let! x{caret} = Async.Sleep(0)
    return ()
} |> ignore
