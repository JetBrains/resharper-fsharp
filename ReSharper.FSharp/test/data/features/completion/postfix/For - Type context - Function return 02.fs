// ${COMPLETE_ITEM:for}
module Module

let f a =
    let g () : int = a
    let b = g ()
    b.{caret}
    ()
