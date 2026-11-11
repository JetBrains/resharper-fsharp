namespace Ns1

[<RequireQualifiedAccess>]
module Module1 =
    [<RequireQualifiedAccess>]
    module Nested1 =
        type T() = class end

namespace N2

module Nested =
    let t = {caret}T()
