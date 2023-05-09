// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

open System
let c = Math.PI
let d = 23
let e = "bar"
let f x y = x * y
[<System.Obsolete>]
let g x = x
{caret}
