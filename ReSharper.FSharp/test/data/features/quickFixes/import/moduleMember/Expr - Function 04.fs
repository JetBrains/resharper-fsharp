namespace Ns1

[<RequireQualifiedAccess>]
module Top =
    let f x = 1

namespace Ns2

module M =
    let i = f{caret}
