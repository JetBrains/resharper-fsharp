namespace Ns1

[<RequireQualifiedAccess>]
module Top =
    let [<Literal>] Literal1 = 1

namespace Ns2

module M =
    match 1 with
    | Literal1{caret} -> ()
