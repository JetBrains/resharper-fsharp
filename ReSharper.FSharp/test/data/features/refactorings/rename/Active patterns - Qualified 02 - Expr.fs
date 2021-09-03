//${NEW_NAME:Zzz}
module Module

module Nested =
    let (|A|_|) x = Some ()

let _ = Nested.(|A{caret}|_|)

let (|Id|) f x = x

match () with
| Id Nested.(|A|_|) () -> ()
