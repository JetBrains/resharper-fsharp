namespace Ns1

module Nested1 =
    [<AutoOpen>]
    module Nested2 =
        [<RequireQualifiedAccess>]
        type U =
            | A of field: int

namespace Ns2

module Nested3 =
    let a{caret} = Ns1.Nested1.Nested2.U.A 1
