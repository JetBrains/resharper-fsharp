module Module

let a1 = async {
    let! (x{caret}: _ list) = async.Return [0]
    ()
}