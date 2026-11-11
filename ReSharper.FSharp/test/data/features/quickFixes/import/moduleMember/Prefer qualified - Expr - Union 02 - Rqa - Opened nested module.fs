namespace Ns1

module Top =
    module Nested =
        [<RequireQualifiedAccess>]
        type Union =
            | Case

namespace Ns2

module M =
    open Ns1.Top.Nested

    let u = Case{caret}
