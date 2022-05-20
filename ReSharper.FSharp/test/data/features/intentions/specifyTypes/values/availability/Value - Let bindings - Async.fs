module Module

let a1 = async {
    let! x{on} = async.Return 0
    ()
}

let a2 = async {
    let! (x{off}: int){off} = async.Return 0
    ()
}

let a3 = async {
    let! (x{on}: _ list){on} = async.Return [0]
    ()
}

let a4 = async {
    let! _{off} = async.Return [0]
    ()
}

let a5 = async {
    let! (_{off}: _ list) = async.Return [0]
    ()
}