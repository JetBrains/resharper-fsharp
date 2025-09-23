// ${ABSENT_ITEM:for}
module Module

let (|Id|) (x: int) = x

let f (Id x) =
    x.{caret}
    ()
