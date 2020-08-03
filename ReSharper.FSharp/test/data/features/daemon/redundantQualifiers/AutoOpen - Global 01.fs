namespace global
[<AutoOpen>]
module Nested1 =
    type T() = class end

namespace Ns
module Nested2 =
    let t: Nested1.T = Nested1.T()

