module Module

type Type() =
    interface IInterface with
        member this.Dispose() = ()

do
    use t = new Type()
    (t :> System.IDisposable).Dispose()
