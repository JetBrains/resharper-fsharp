// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

open System
let c = Math.PI
let d = 23
let e = "bar"
let f x y = x * y
[<BAttribute(MyEnum.A)>]
[<System.Obsolete "foo">]
[<System.NonSerialized>]
let g x = x
let [<Literal>] Hello = "Hello"
[<CompiledName(Hello)>]
let hello = 1
{caret}
