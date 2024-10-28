namespace Ns1

[<RequireQualifiedAccess>]
module Top =
    [<AutoOpen>]
    module Nested =
        [<RequireQualifiedAccess>]
        type Union =
            | Case

namespace Ns2

module M =
    let u = Case{caret}
