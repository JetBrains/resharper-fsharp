module Module

type T() =
    use d = { new System.IDisposable with
                 member x.Dispose() = 123; () }
    let x = 123
