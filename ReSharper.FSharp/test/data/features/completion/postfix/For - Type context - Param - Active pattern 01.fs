// ${COMPLETE_ITEM:for}
module Module

let (|Id|) x = x

let f (Id x) =
    x.{caret}
    ()
