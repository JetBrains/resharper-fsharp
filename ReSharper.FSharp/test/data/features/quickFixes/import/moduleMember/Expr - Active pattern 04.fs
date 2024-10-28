namespace Ns1

[<RequireQualifiedAccess>]
module Top =
    let (|Aaa|_|) x = if x then Some() else None

namespace Ns2

module M =
    let i = (| Aaa | _ |){caret}
