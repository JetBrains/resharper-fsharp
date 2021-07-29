namespace Ns1

[<RequireQualifiedAccess>]
module Module =
    type U =
        | A of int

namespace Ns2

module Module2 =
    let a{caret} = Ns1.Module.A 1
