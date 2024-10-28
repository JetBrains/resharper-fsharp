namespace Ns1

[<AutoOpen>]
module Top =
    [<RequireQualifiedAccess>]
    module Nested =
        let [<Literal>] Literal1 = 1

namespace Ns2

module M =
    match 1 with
    | Literal1{caret} -> ()
