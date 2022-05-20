module Module

let a1 = async {
    let! (x, y{caret}) = async.Return ("", 0)
    ()
}