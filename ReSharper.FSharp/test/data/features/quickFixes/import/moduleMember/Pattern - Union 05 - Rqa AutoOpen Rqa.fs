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
    match Unchecked.defaultof<Ns1.Top.Nested.Union> with
    | Case{caret} -> ()
