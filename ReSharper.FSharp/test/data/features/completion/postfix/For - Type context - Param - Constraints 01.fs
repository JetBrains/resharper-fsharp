// ${ABSENT_ITEM:for}
module Module

open System

let f (x: #IDisposable) =
    x.{caret}
    ()
