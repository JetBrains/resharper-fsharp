module Module

async {
    let!{off} x{off}: int = async { return 1 }
    and!{off} y{off}: int = async { return 2 }

    let!{off} (a{off}: int) = async { return 1 }
    and!{off} (b{off}: int) = async { return 2 }
    ()
}
