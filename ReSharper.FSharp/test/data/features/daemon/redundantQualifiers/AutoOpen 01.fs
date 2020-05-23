namespace Ns1
[<AutoOpen>]
module Nested1 =
    let x = 123

namespace Ns2
module Nested2 =
    open Ns1
    Nested1.x
