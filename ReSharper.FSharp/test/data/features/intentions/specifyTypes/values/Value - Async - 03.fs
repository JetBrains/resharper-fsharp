module Module

let a1 = async {
    let! (x{caret}: _ * _) = async.Return ("", 0)
    ()
}