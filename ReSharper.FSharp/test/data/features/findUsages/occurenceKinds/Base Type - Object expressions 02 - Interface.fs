module Module

let t = {
    new System.IDisposable with
        override x.Dispose() = ()
}
