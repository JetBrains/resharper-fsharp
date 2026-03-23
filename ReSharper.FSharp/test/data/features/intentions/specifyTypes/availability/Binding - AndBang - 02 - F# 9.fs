module Module

async {
    let!{off} x{off} = async { return 1 }
    and!{off} y{off} = async { return 2 }
    ()
}
