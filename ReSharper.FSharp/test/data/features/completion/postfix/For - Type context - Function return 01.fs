// ${COMPLETE_ITEM:for}
module Module

let f a =
    let g () = a
    let b = g ()
    b.{caret}
    ()
