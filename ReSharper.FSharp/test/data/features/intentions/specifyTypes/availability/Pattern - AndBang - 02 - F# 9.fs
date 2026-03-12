module Module

async {
    let!{off} x{on} = async { return 1 }
    and!{off} y{on} = async { return 2 }
    ()
}
