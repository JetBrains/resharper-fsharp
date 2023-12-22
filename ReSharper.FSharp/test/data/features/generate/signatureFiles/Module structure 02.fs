// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

open System
let c = Math.PI
let d = 23
let e = "bar"
let f x y = x * y
/// comment that should be preserved
[<BAttribute(MyEnum.A)>]  // comment that should disappear 1
[<System.Obsolete "foo">]
[<System.NonSerialized>]
let g x = x
let [<Literal>] (* comment that should disappear 2 *) Hello = "Hello"
[<CompiledName(Hello)>]
let hello = 1
[<A; B>]
let h = 23
{caret}
