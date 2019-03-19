module Module

type T() =
    inherit System.Object()

    let x = 123
    let f x = x + 1

    member x.Foo = 123

    interface System.IDisposable with
        member x.Dispose() = ()
