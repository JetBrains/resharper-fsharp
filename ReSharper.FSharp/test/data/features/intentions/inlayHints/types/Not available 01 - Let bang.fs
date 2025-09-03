module Module

async {
    let! x{caret} = Async.Sleep(50)
    return x
}
