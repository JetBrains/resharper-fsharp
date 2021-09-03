//${NEW_NAME:Zzz}
module Module

module Nested =
    let (|A{caret}|_|) x = Some ()

let _ = Nested.(|A|_|)

let (|Id|) f x = x

match () with
| Id Nested.(|A|_|) () -> ()
