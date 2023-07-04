module Module

async {
    use! x{off} = async { return { new System.IDisposable with member __.Dispose() = () } }
    return ()
}
