do
    let{on} d{on} = { new System.IDisposable with member x.Dispose() = () }
    let{on} a{off} as b{on} = { new System.IDisposable with member x.Dispose() = () }
    let{off} a{off}, b{off} =
        { new System.IDisposable with member x.Dispose() = () },
        { new System.IDisposable with member x.Dispose() = () }
