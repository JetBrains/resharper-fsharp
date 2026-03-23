module Module

async {
    let!{off} x{off}: int = async { return 1 }
    let!{off} (y{off}: int) = async { return 2 }
    return ()
}
